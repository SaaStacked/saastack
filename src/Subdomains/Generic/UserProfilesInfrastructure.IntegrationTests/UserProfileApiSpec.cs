using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using Common;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.Images;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace UserProfilesInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class UserProfileApiSpec : WebApiSpec<Program>
{
    public UserProfileApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenRegisterWithCredentials_ThenCreatesProfile()
    {
        await Api.PostAsync(new RegisterPersonCredentialRequest
        {
            EmailAddress = "auser@company.com",
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            Timezone = "Australia/Sydney",
            Locale = "en-AU",
            CountryCode = "AU",
            TermsAndConditionsAccepted = true
        });

        await PropagateDomainEventsAsync();
        var token = UserNotificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmPersonCredentialRegistrationRequest
        {
            Token = token
        });

        await PropagateDomainEventsAsync();
        var tokens = (await Api.PostAsync(new AuthenticateCredentialRequest
        {
            Username = "auser@company.com",
            Password = "1Password!"
        })).Content.Value.Tokens;

        var result = await Api.GetAsync(new GetProfileForCallerRequest(),
            req => req.SetJWTBearerToken(tokens.AccessToken.Value));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Profile.IsAuthenticated.Should().BeTrue();
        result.Content.Value.Profile.Id.Should().NotBeNullOrEmpty();
        result.Content.Value.Profile.UserId.Should().Be(tokens.UserId);
        result.Content.Value.Profile.DefaultOrganizationId.Should().NotBeNullOrEmpty();
        result.Content.Value.Profile.Name.FirstName.Should().Be("afirstname");
        result.Content.Value.Profile.Name.LastName.Should().Be("alastname");
        result.Content.Value.Profile.DisplayName.Should().Be("afirstname");
        result.Content.Value.Profile.Timezone.Should().Be("Australia/Sydney");
        result.Content.Value.Profile.AvatarUrl.Should().BeNull();
        result.Content.Value.Profile.Classification.Should().Be(UserProfileClassification.Person);
        result.Content.Value.Profile.Roles.Should().OnlyContain(role => role == PlatformRoles.Standard.Name);
        result.Content.Value.Profile.Features.Should()
            .ContainInOrder(PlatformFeatures.PaidTrial.Name, PlatformFeatures.Basic.Name);
        result.Content.Value.Profile.EmailAddress.Should().Be("auser@company.com");
        result.Content.Value.Profile.Locale.Should().Be("en-AU");
        result.Content.Value.Profile.PhoneNumber.Should().BeNull();
        result.Content.Value.Profile.Address.Line1.Should().BeEmpty();
        result.Content.Value.Profile.Address.Line2.Should().BeEmpty();
        result.Content.Value.Profile.Address.Line3.Should().BeEmpty();
        result.Content.Value.Profile.Address.City.Should().BeEmpty();
        result.Content.Value.Profile.Address.State.Should().BeEmpty();
        result.Content.Value.Profile.Address.CountryCode.Should().Be("AUS");
        result.Content.Value.Profile.Address.Zip.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenChangeProfile_ThenChanges()
    {
        var login = await LoginUserAsync();

        var result = await Api.PatchAsync(new ChangeProfileRequest
        {
            UserId = login.User.Id,
            FirstName = "anewfirstname",
            LastName = "anewlastname",
            DisplayName = "anewdisplayname",
            Timezone = Timezones.Sydney.ToString()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Profile.Name.FirstName.Should().Be("anewfirstname");
        result.Content.Value.Profile.Name.LastName.Should().Be("anewlastname");
        result.Content.Value.Profile.DisplayName.Should().Be("anewdisplayname");
        result.Content.Value.Profile.Timezone.Should().Be(Timezones.Sydney.ToString());
        result.Content.Value.Profile.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task WhenChangeProfileByAnotherUser_ThenForbidden()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var result = await Api.PatchAsync(new ChangeProfileRequest
        {
            UserId = loginA.User.Id,
            FirstName = "anewfirstname",
            LastName = "anewlastname",
            DisplayName = "anewdisplayname",
            Timezone = Timezones.Sydney.ToString()
        }, req => req.SetJWTBearerToken(loginB.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WhenChangeContactAddress_ThenChanges()
    {
        var login = await LoginUserAsync();

        var result = await Api.PatchAsync(new ChangeProfileContactAddressRequest
        {
            UserId = login.User.Id,
            Line1 = "anewline1",
            Line2 = "anewline2",
            Line3 = "anewline3",
            City = "anewcity",
            State = "anewstate",
            CountryCode = CountryCodes.Australia.ToString(),
            Zip = "anewzipcode"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Profile.Address.Line1.Should().Be("anewline1");
        result.Content.Value.Profile.Address.Line2.Should().Be("anewline2");
        result.Content.Value.Profile.Address.Line3.Should().Be("anewline3");
        result.Content.Value.Profile.Address.City.Should().Be("anewcity");
        result.Content.Value.Profile.Address.State.Should().Be("anewstate");
        result.Content.Value.Profile.Address.CountryCode.Should().Be(CountryCodes.Australia.ToString());
        result.Content.Value.Profile.Address.Zip.Should().Be("anewzipcode");
    }

    [Fact]
    public async Task WhenChangeContactAddressByAnotherUser_ThenForbidden()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var result = await Api.PatchAsync(new ChangeProfileContactAddressRequest
        {
            UserId = loginA.User.Id,
            Line1 = "anewline1",
            Line2 = "anewline2",
            Line3 = "anewline3",
            City = "anewcity",
            State = "anewstate",
            CountryCode = CountryCodes.Australia.ToString(),
            Zip = "anewzipcode"
        }, req => req.SetJWTBearerToken(loginB.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WhenChangeAvatarByAnotherUser_ThenForbidden()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var result = await Api.PutAsync(new ChangeProfileAvatarRequest
            {
                UserId = loginA.User.Id
            }, new PostFile(GetTestImage(), HttpConstants.ContentTypes.ImagePng),
            req => req.SetJWTBearerToken(loginB.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WhenChangeAvatar_ThenChanges()
    {
        var login = await LoginUserAsync();

        var result = await Api.PutAsync(new ChangeProfileAvatarRequest
            {
                UserId = login.User.Id
            }, new PostFile(GetTestImage(), HttpConstants.ContentTypes.ImagePng),
            req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Profile.AvatarUrl.Should().StartWith("https://localhost:5001/images/image_");
    }

    [Fact]
    public async Task WhenDeleteAvatar_ThenDeletes()
    {
        var login = await LoginUserAsync();

        await Api.PutAsync(new ChangeProfileAvatarRequest
            {
                UserId = login.User.Id
            }, new PostFile(GetTestImage(), HttpConstants.ContentTypes.ImagePng),
            req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.DeleteAsync(new DeleteProfileAvatarRequest
        {
            UserId = login.User.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Profile.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task WhenGetProfileForCallerForAnonymous_ThenNotAuthenticated()
    {
        var result = await Api.GetAsync(new GetProfileForCallerRequest());

        result.Content.Value.Profile.IsAuthenticated.Should().BeFalse();
        result.Content.Value.Profile.Id.Should().Be(CallerConstants.AnonymousUserId);
        result.Content.Value.Profile.UserId.Should().Be(CallerConstants.AnonymousUserId);
        result.Content.Value.Profile.DefaultOrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task WhenGetProfileForCallerForAuthenticated_ThenAuthenticated()
    {
        var login = await LoginUserAsync();

        var result = await Api.GetAsync(new GetProfileForCallerRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Profile.IsAuthenticated.Should().BeTrue();
        result.Content.Value.Profile.Id.Should().NotBeNullOrEmpty();
        result.Content.Value.Profile.UserId.Should().Be(login.User.Id);
        result.Content.Value.Profile.DefaultOrganizationId.Should().NotBeNullOrEmpty();
        result.Content.Value.Profile.Name.FirstName.Should().Be("persona");
        result.Content.Value.Profile.Name.LastName.Should().Be("alastname");
        result.Content.Value.Profile.DisplayName.Should().Be("persona");
        result.Content.Value.Profile.Timezone.Should().Be(Timezones.Default.ToString());
        result.Content.Value.Profile.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task WhenDeleteImageBehindTheAvatar_ThenRemovesAvatar()
    {
        var login = await LoginUserAsync();

        var profile = await Api.PutAsync(new ChangeProfileAvatarRequest
            {
                UserId = login.User.Id
            }, new PostFile(GetTestImage(), HttpConstants.ContentTypes.ImagePng),
            req => req.SetJWTBearerToken(login.AccessToken));

        var avatarUrl = profile.Content.Value.Profile.AvatarUrl!;
        var imageId = avatarUrl
            .Replace("https://localhost:5001/images/", string.Empty)
            .Replace("/download", string.Empty);

        await PropagateDomainEventsAsync();
        await Api.DeleteAsync(new DeleteImageRequest
        {
            Id = imageId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        await PropagateDomainEventsAsync();
        var result = await Api.GetAsync(new GetProfileForCallerRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Profile.AvatarUrl.Should().BeNull();
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // do nothing
    }
}