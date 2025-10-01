# Documentation

[TOC]

## Getting Started

This repository is a template. The main idea is that you copy (or fork) the whole repo, modify it to be yours, and use it to start building out your new product.

Sign up to the [Standard Edition of SaaStacked](https://www.saastacked.com) and download all the technology adapters that you will need to suit your specific cloud deployment on AWS,  Azure or Google Cloud and choice of 3rd party services.

Once there, we will guide you through the process of cloning and owning the entire codebase, making it your product, installing your adapters, and getting it build, tested and deployed within your first day!

## All Use cases

Take a look at all the [all use cases](design-principles/0000-all-use-cases.md) for this software, there are already over ~130 public APIs already built for you, that you won't have to build over again.

We have also added a fully operational web application (built in React.js) that you can use to get your product off the ground today!

It has many of the pages already shipped for you, you just have to add your own!

Don't spend weeks and months building your own JavaScript App and API from scratch, it is already there, ready to go. 

## Architecture Design Records

Browse our [Architectural Design Records](decisions/README.md) that describe the key assumptions and decisions made behind some of the bigger decisions made in this code template.

## Design Principles

Learn about the [Design Principles and Implementation details](design-principles/README.md) of the code template. What we aimed to achieve, and how we go about doing it.

## How-To Guides

Read the [How To Guides](how-to-guides/README.md) to help you get started and perform the most common tasks in working with and customizing this codebase template to suit your product needs.

# More Documentation

In the Standard edition we have further documentation for extensive tooling, coding standards to pre-program your AI tools, and documentation to deploy everything on your chose Cloud Platform. 

Sign up to the [Standard Edition of SaaStacked](https://www.saastacked.com) to receive these benefits.

### Tooling

As well as a code template, there is custom tooling (tailored to this codebase) to guide you to being more productive with using this template.

We make extensive use Roslyn Analyzers, Code Fixes and Source Generators and Architecture tests to help you and your team be highly productive in following the established patterns this codebase. And more importantly detect and fix when those principles are violated.

For example, we make it trivial to define robust REST APIs, and under the covers, the tooling converts those API definitions into minimal APIs for you. But you never have to write all that minimal API boilerplate stuff, or worry about how it is organized in code. This is all hidden away from you, not requiring any input from you.

Furthermore, we have many Roslyn analyzers that continuously check your code to make sure that you and your team do not inadvertently violate certain architectural rules and constraints that are in place to manage the complexity of the source code over time, as your product develops. It is like having continuous code review, and your own plugins that understand your code.

For example, clean architecture mandates that all dependencies point inwards from Infrastructure to Application to Domain layers for very good reasons. But developers, in the heat of the moment, can easily violate that rule by simply adding a `using` statement the other way. Your IDE will make this easy for you, and it doesn't actually care, because it does not care about architectural rules. Your code review process may miss that subtle violation. However, our Roslyn rules certainly won't miss that violation, and they will guide you into fixing it.

Lastly, if you are using JetBrains Rider, we have baked in a set of common coding standards that are enforced across the codebase for you.
We also provide you with a number of project templates for adding the various projects for new subdomains.
We also give you several macros in the text editor (a.k.a. Live Templates) for creating certain kinds of classes, like DDD ValueObjects and DDD AggregateRoots, and xUnit test classes.

You can see all of these things in the Framework/Tools projects.

### Deployment

The codebase is ready for deployment immediately, from your GitHub repository.
Deployment can be performed by any tool set to any environment, any way you like, we just made it easy for GitHub actions.

### Coding Standards

We have extensive documentation on coding standards, not just for your team to read, but also in a format that you can point your favorite AI tools to read and learn about the codebase.

In addition, we support additional memory and rules for files for specific AI tools like [Augment Code](https://www.augmentcode.com/).