extern alias Generators;
using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Generators::Application.Interfaces;
using Generators::Domain.Interfaces.Authorization;
using Generators::Infrastructure.Web.Api.Interfaces;
using Generators::JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Xunit;
using WebApiAssemblyVisitor = Generators::Tools.Generators.Web.Api.WebApiAssemblyVisitor;

namespace Tools.Generators.Web.Api.UnitTests;

[UsedImplicitly]
public class WebApiAssemblyVisitorSpec
{
    private const string CompilationSourceCode = "";
    private static readonly string[] AdditionalCompilationAssemblies =
    [
        "System.Runtime.dll",
        "netstandard.dll"
    ]; //HACK: required to analyze use custom attributes

    private static CSharpCompilation CreateCompilation(string sourceCode)
    {
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(WebApiAssemblyVisitor).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)
        };
        AdditionalCompilationAssemblies.ToList()
            .ForEach(item => references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, item))));
        var compilation = CSharpCompilation.Create("compilation",
            [
                CSharpSyntaxTree.ParseText(sourceCode)
            ],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        return compilation;
    }

    [Trait("Category", "Unit.Tooling")]
    public class GivenAnyClass
    {
        private readonly WebApiAssemblyVisitor _visitor;

        public GivenAnyClass()
        {
            var compilation = CreateCompilation(CompilationSourceCode);
            _visitor = new WebApiAssemblyVisitor(compilation, CancellationToken.None);
        }

        [Fact]
        public void WhenVisitAssembly_ThenVisitsGlobalNamespace()
        {
            var globalNamespace = new Mock<INamespaceSymbol>();
            var assembly = new Mock<IAssemblySymbol>();
            assembly.Setup(ass => ass.GlobalNamespace).Returns(globalNamespace.Object);
            _visitor.VisitAssembly(assembly.Object);

            globalNamespace.Verify(gns => gns.Accept(_visitor));
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamespaceAndIgnoredNamespace_ThenStopsVisiting()
        {
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.Setup(ns => ns.Name).Returns(WebApiAssemblyVisitor.IgnoredNamespaces[0]);

            _visitor.VisitNamespace(@namespace.Object);

            @namespace.Verify(gns => gns.GetMembers(), Times.Never);
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamespaceAndNoTypes_ThenStopsVisiting()
        {
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.Setup(ns => ns.Name).Returns("anamespace");
            @namespace.Setup(ns => ns.GetMembers()).Returns(new List<INamespaceOrTypeSymbol>());

            _visitor.VisitNamespace(@namespace.Object);

            @namespace.Verify(gns => gns.GetMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamespaceThatHasTypes_ThenVisitsTypes()
        {
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.Setup(ns => ns.Name).Returns("anamespace");
            var type = new Mock<INamespaceOrTypeSymbol>();
            @namespace.Setup(ns => ns.GetMembers()).Returns(new List<INamespaceOrTypeSymbol>
            {
                type.Object
            });

            _visitor.VisitNamespace(@namespace.Object);

            @namespace.Verify(gns => gns.GetMembers());
            type.Verify(t => t.Accept(_visitor));
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndNotServiceClass_ThenStopsVisiting()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers())
                .Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Interface);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsClassButNotPublic_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers())
                .Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Private);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButAlsoAbstract_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsAbstract).Returns(true);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButAlsoStatic_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsStatic).Returns(true);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButNotAnyBaseType_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsStatic).Returns(false);
            type.Setup(t => t.IsAbstract).Returns(false);
            type.Setup(t => t.AllInterfaces).Returns(ImmutableArray<INamedTypeSymbol>.Empty);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButWrongBaseType_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            var classBaseType = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.IsStatic).Returns(false);
            type.Setup(t => t.IsAbstract).Returns(false);
            type.Setup(t => t.AllInterfaces).Returns([classBaseType.Object]);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }
    }

    [Trait("Category", "Unit.Tooling")]
    public class GivenAServiceClass
    {
        private readonly CSharpCompilation _compilation;
        private readonly WebApiAssemblyVisitor _visitor;

        public GivenAServiceClass()
        {
            _compilation = CreateCompilation(CompilationSourceCode);
            _visitor = new WebApiAssemblyVisitor(_compilation, CancellationToken.None);
        }

        [Fact]
        public void WhenVisitNamedTypeAndHasNoMethods_ThenCreatesNoRegistrations()
        {
            var type = SetupServiceClass(_compilation);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndOnlyPrivateMethod_ThenCreatesNoRegistrations()
        {
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Private);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndOnlyPublicStaticMethod_ThenCreatesNoRegistrations()
        {
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(true);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndVoidReturnType_ThenCreatesNoRegistrations()
        {
            var voidMetadata = _compilation.GetTypeByMetadataName(typeof(void).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(voidMetadata);
            type.Setup(t => t.GetMembers()).Returns([method.Object]);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndOperationHasNoParameters_ThenCreatesNoRegistrations()
        {
            var taskMetadata = _compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(taskMetadata);
            method.Setup(m => m.Parameters).Returns([]);
            type.Setup(t => t.GetMembers()).Returns([method.Object]);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndOperationHasWrongFirstParameter_ThenCreatesNoRegistrations()
        {
            var taskMetadata = _compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(taskMetadata);
            var parameter = new Mock<IParameterSymbol>();
            var classBaseType = new Mock<INamedTypeSymbol>();
            parameter.Setup(p => p.Type.AllInterfaces).Returns([classBaseType.Object]);
            method.Setup(m => m.Parameters).Returns([parameter.Object]);
            type.Setup(t => t.GetMembers()).Returns([method.Object]);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndOperationHasWrongSecondParameter_ThenCreatesNoRegistrations()
        {
            var requestMetadata = _compilation.GetTypeByMetadataName(typeof(IWebRequest).FullName!)!;
            var stringMetadata = _compilation.GetTypeByMetadataName(typeof(string).FullName!)!;
            var taskMetadata = _compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(taskMetadata);
            var firstParameter = new Mock<IParameterSymbol>();
            firstParameter.Setup(p => p.Type.AllInterfaces).Returns([requestMetadata]);
            var secondParameter = new Mock<IParameterSymbol>();
            secondParameter.Setup(p => p.Type).Returns(stringMetadata);
            method.Setup(m => m.Parameters)
                .Returns([firstParameter.Object, secondParameter.Object]);
            type.Setup(t => t.GetMembers()).Returns([method.Object]);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndRequestDtoHasNoAttributes_ThenCreatesNoRegistrations()
        {
            var requestMetadata = _compilation.GetTypeByMetadataName(typeof(IWebRequest).FullName!)!;
            var cancellationTokenMetadata = _compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)!;
            var taskMetadata = _compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(taskMetadata);
            var firstParameter = new Mock<IParameterSymbol>();
            firstParameter.Setup(p => p.Type.GetAttributes()).Returns([]);
            firstParameter.Setup(p => p.Type.AllInterfaces).Returns([requestMetadata]);
            var secondParameter = new Mock<IParameterSymbol>();
            secondParameter.Setup(p => p.Type).Returns(cancellationTokenMetadata);
            method.Setup(m => m.Parameters)
                .Returns([firstParameter.Object, secondParameter.Object]);
            type.Setup(t => t.GetMembers()).Returns([method.Object]);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenAServiceOperation
        {
            [Fact]
            public void WhenVisitNamedTypeAndRequestDtoHasRouteAttribute_ThenCreatesRegistration()
            {
                var compilation = CreateCompilation("""
                                                    using System;
                                                    using Infrastructure.Web.Api.Interfaces;

                                                    namespace ANamespace;

                                                    public class AResponse : IWebResponse
                                                    {
                                                    }
                                                    [Infrastructure.Web.Api.Interfaces.RouteAttribute("aroute", OperationMethod.Get)]
                                                    public class ARequest : WebRequest<ARequest, AResponse>
                                                    {
                                                    }
                                                    public class AServiceClass : Infrastructure.Web.Api.Interfaces.IWebApiService
                                                    {
                                                        public string AMethod(ARequest request)
                                                        {
                                                             return "";
                                                        }
                                                    }
                                                    """);

                var serviceClass = compilation.GetTypeByMetadataName("ANamespace.AServiceClass")!;
                var visitor = new WebApiAssemblyVisitor(compilation, CancellationToken.None);

                visitor.VisitNamedType(serviceClass);

                visitor.OperationRegistrations.Count.Should().Be(1);
                var registration = visitor.OperationRegistrations.First();
                registration.Class.Constructors.Count().Should().Be(1);
                registration.Class.Constructors.First().CtorParameters.Count().Should().Be(0);
                registration.Class.Constructors.First().IsInjectionCtor.Should().BeFalse();
                registration.Class.Constructors.First().MethodBody.Should().BeEmpty();
                registration.Class.TypeName.Name.Should().Be("AServiceClass");
                registration.Class.TypeName.Namespace.Should().Be("ANamespace");
                registration.Class.TypeName.FullName.Should().Be("global::ANamespace.AServiceClass");
                registration.Class.UsingNamespaces.Count().Should().Be(2);
                registration.MethodBody.Should().Be($"    {{{Environment.NewLine}"
                                                    + $"         return \"\";{Environment.NewLine}"
                                                    + $"    }}{Environment.NewLine}");
                registration.MethodName.Should().Be("AMethod");
                registration.OperationMethod.Should().Be(OperationMethod.Get);
                registration.OperationAuthorization.Should().BeNull();
                registration.RoutePath.Should().Be("aroute");
                registration.IsTestingOnly.Should().BeFalse();
                registration.RequestDto.Name.Should().Be("ARequest");
                registration.RequestDto.Namespace.Should().Be("ANamespace");
                registration.ResponseDto.Name.Should().Be("AResponse");
                registration.ResponseDto.Namespace.Should().Be("ANamespace");
            }

            [Fact]
            public void WhenVisitNamedTypeAndRequestDtoHasASingleAuthorizeAttribute_ThenCreatesRegistration()
            {
                var compilation = CreateCompilation("""
                                                    using System;
                                                    using Infrastructure.Web.Api.Interfaces;

                                                    namespace ANamespace;

                                                    public class AResponse : IWebResponse
                                                    {
                                                    }
                                                    [Infrastructure.Web.Api.Interfaces.AuthorizeAttribute(Infrastructure.Web.Api.Interfaces.Roles.Platform_Standard, Infrastructure.Web.Api.Interfaces.Features.Platform_Basic)]
                                                    [Infrastructure.Web.Api.Interfaces.RouteAttribute("aroute", OperationMethod.Get)]
                                                    public class ARequest : WebRequest<ARequest, AResponse>
                                                    {
                                                    }
                                                    public class AServiceClass : Infrastructure.Web.Api.Interfaces.IWebApiService
                                                    {
                                                        public string AMethod(ARequest request)
                                                        {
                                                             return "";
                                                        }
                                                    }
                                                    """);

                var serviceClass = compilation.GetTypeByMetadataName("ANamespace.AServiceClass")!;
                var visitor = new WebApiAssemblyVisitor(compilation, CancellationToken.None);

                visitor.VisitNamedType(serviceClass);

                visitor.OperationRegistrations.Count.Should().Be(1);
                var registration = visitor.OperationRegistrations.First();
                registration.Class.Constructors.Count().Should().Be(1);
                registration.Class.Constructors.First().CtorParameters.Count().Should().Be(0);
                registration.Class.Constructors.First().IsInjectionCtor.Should().BeFalse();
                registration.Class.Constructors.First().MethodBody.Should().BeEmpty();
                registration.Class.TypeName.Name.Should().Be("AServiceClass");
                registration.Class.TypeName.Namespace.Should().Be("ANamespace");
                registration.Class.TypeName.FullName.Should().Be("global::ANamespace.AServiceClass");
                registration.Class.UsingNamespaces.Count().Should().Be(2);
                registration.MethodBody.Should().Be($"    {{{Environment.NewLine}"
                                                    + $"         return \"\";{Environment.NewLine}"
                                                    + $"    }}{Environment.NewLine}");
                registration.MethodName.Should().Be("AMethod");
                registration.OperationMethod.Should().Be(OperationMethod.Get);
                registration.OperationAuthorization!.PolicyName.Should().Be(
                    $"{AuthenticationConstants.Authorization.RolesAndFeaturesPolicyName}:{{|Features|:{{|Platform|:[|{PlatformFeatures.Basic.Name}|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}");
                registration.RoutePath.Should().Be("aroute");
                registration.IsTestingOnly.Should().BeFalse();
                registration.RequestDto.Name.Should().Be("ARequest");
                registration.RequestDto.Namespace.Should().Be("ANamespace");
                registration.ResponseDto.Name.Should().Be("AResponse");
                registration.ResponseDto.Namespace.Should().Be("ANamespace");
            }

            [Fact]
            public void WhenVisitNamedTypeAndRequestDtoHasManyAuthorizeAttributes_ThenCreatesRegistration()
            {
                var compilation = CreateCompilation("""
                                                    using System;
                                                    using Infrastructure.Web.Api.Interfaces;

                                                    namespace ANamespace;

                                                    public class AResponse : IWebResponse
                                                    {
                                                    }
                                                    [Infrastructure.Web.Api.Interfaces.AuthorizeAttribute(Infrastructure.Web.Api.Interfaces.Roles.Platform_Operations, Infrastructure.Web.Api.Interfaces.Features.Platform_Paid2)]
                                                    [Infrastructure.Web.Api.Interfaces.AuthorizeAttribute(Infrastructure.Web.Api.Interfaces.Roles.Platform_Standard, Infrastructure.Web.Api.Interfaces.Features.Platform_Basic)]
                                                    [Infrastructure.Web.Api.Interfaces.RouteAttribute("aroute", OperationMethod.Get)]
                                                    public class ARequest : WebRequest<ARequest, AResponse>
                                                    {
                                                    }
                                                    public class AServiceClass : Infrastructure.Web.Api.Interfaces.IWebApiService
                                                    {
                                                        public string AMethod(ARequest request)
                                                        {
                                                             return "";
                                                        }
                                                    }
                                                    """);

                var serviceClass = compilation.GetTypeByMetadataName("ANamespace.AServiceClass")!;
                var visitor = new WebApiAssemblyVisitor(compilation, CancellationToken.None);

                visitor.VisitNamedType(serviceClass);

                visitor.OperationRegistrations.Count.Should().Be(1);
                var registration = visitor.OperationRegistrations.First();
                registration.Class.Constructors.Count().Should().Be(1);
                registration.Class.Constructors.First().CtorParameters.Count().Should().Be(0);
                registration.Class.Constructors.First().IsInjectionCtor.Should().BeFalse();
                registration.Class.Constructors.First().MethodBody.Should().BeEmpty();
                registration.Class.TypeName.Name.Should().Be("AServiceClass");
                registration.Class.TypeName.Namespace.Should().Be("ANamespace");
                registration.Class.TypeName.FullName.Should().Be("global::ANamespace.AServiceClass");
                registration.Class.UsingNamespaces.Count().Should().Be(2);
                registration.MethodBody.Should().Be($"    {{{Environment.NewLine}"
                                                    + $"         return \"\";{Environment.NewLine}"
                                                    + $"    }}{Environment.NewLine}");
                registration.MethodName.Should().Be("AMethod");
                registration.OperationMethod.Should().Be(OperationMethod.Get);
                registration.OperationAuthorization!.PolicyName.Should().Be(
                    $"{AuthenticationConstants.Authorization.RolesAndFeaturesPolicyName}:{{|Features|:{{|Platform|:[|{PlatformFeatures.Paid2.Name}|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Operations.Name}|]}}}}"
                    + $"{AuthenticationConstants.Authorization.RolesAndFeaturesPolicyName}:{{|Features|:{{|Platform|:[|{PlatformFeatures.Basic.Name}|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}");
                registration.RoutePath.Should().Be("aroute");
                registration.IsTestingOnly.Should().BeFalse();
                registration.RequestDto.Name.Should().Be("ARequest");
                registration.RequestDto.Namespace.Should().Be("ANamespace");
                registration.ResponseDto.Name.Should().Be("AResponse");
                registration.ResponseDto.Namespace.Should().Be("ANamespace");
            }

            [Fact]
            public void WhenVisitNamedTypeAndClassHasBaseApiFromAttribute_ThenCreatesRegistration()
            {
                var compilation = CreateCompilation("""
                                                    using System;
                                                    using Infrastructure.Web.Api.Interfaces;

                                                    namespace ANamespace;

                                                    public class AResponse : IWebResponse
                                                    {
                                                    }
                                                    [Infrastructure.Web.Api.Interfaces.RouteAttribute("aroute", OperationMethod.Get)]
                                                    public class ARequest : WebRequest<ARequest, AResponse>
                                                    {
                                                    }
                                                    [Infrastructure.Web.Api.Interfaces.BaseApiFromAttribute("aprefix")]
                                                    public class AServiceClass : Infrastructure.Web.Api.Interfaces.IWebApiService
                                                    {
                                                        public string AMethod(ARequest request)
                                                        {
                                                             return "";
                                                        }
                                                    }
                                                    """);

                var serviceClass = compilation.GetTypeByMetadataName("ANamespace.AServiceClass")!;
                var visitor = new WebApiAssemblyVisitor(compilation, CancellationToken.None);

                visitor.VisitNamedType(serviceClass);

                visitor.OperationRegistrations.Count.Should().Be(1);
                var registration = visitor.OperationRegistrations.First();
                registration.Class.BasePath.Should().Be("aprefix");
            }
        }

        private static Mock<INamedTypeSymbol> SetupServiceClass(CSharpCompilation compilation)
        {
            var serviceClassMetadata = compilation.GetTypeByMetadataName(typeof(IWebApiService).FullName!)!;
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsStatic).Returns(false);
            type.Setup(t => t.IsAbstract).Returns(false);
            type.Setup(t => t.AllInterfaces).Returns([serviceClassMetadata]);
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.As<ISymbol>().Setup(ns => ns.ToDisplayString(It.IsAny<SymbolDisplayFormat?>()))
                .Returns("adisplaystring");
            type.Setup(t => t.ContainingNamespace).Returns(@namespace.Object);
            type.Setup(t => t.Name).Returns("aname");
            type.Setup(t => t.GetAttributes()).Returns(ImmutableArray<AttributeData>.Empty);

            return type;
        }
    }
}