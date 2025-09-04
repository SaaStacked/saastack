# Javascript Framework

* status: accepted
* date: 2025-09-04
* deciders:jezzsantos

# Context and Problem Statement

Building front ends for SaaStack applications should be done using a Javascript framework, as opposed to building without a JavaScript framework, since we want to maximize adoption of the codebase, and most developers are familiar with JavaScript and Typescript.

There are two popular choices, one is React.js, the other is Vue.js, each with their own opinions, communities and tooling.

Furthermore, there are also decisions to be made about the bundling and building of the JavaScript code, and the testing framework.

There are many conflicting comparisons and opinions about these. We need to choose one.

## Considered Options

The options are:
1. `React.js + Vite`
2. `React.js + Webpack + Jest`
3. `Vue.js + Vite`

>We assume that Vue.js + Webpack is not a popular enough choice

## Decision Outcome

`React.js + Vite` (and ViteTest)
- React.js is still considered the most popular and well-supported framework for building web applications.
- Vite is considered an improved version of Webpack, and CreateReactApp is long depreciated
- ViteTest is a natural choice for testing, since it is integrated with Vite