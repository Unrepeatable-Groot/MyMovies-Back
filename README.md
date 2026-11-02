# MyMovies API

MyMovies API is the backend for a Georgian movie streaming catalog. It powers a website where visitors can browse films, series and animations, create an account, comment on titles, like or dislike them and save their favorites. Administrators manage the catalog through the same API.

## What It Does

Anyone can browse the catalog without signing in. The API can list every title, filter by category, search by name, return a fixed number of titles, paginate results, and serve the latest additions, the most popular titles (ranked by likes) and a random selection.

Registered users can like or dislike a title, change or remove that reaction, and keep a personal list of favorites capped at 50 titles. They can also write comments, edit their own comments and delete them. Admins and the Owner can delete any comment but cannot edit other people's comments. Users can update their username and profile picture, change their email and change their password.

Admins and the Owner can add, edit and delete titles, including adding many titles in a single bulk request. Only the Owner and Admins can change another user's role. There are three roles: User, Admin and Owner.

## Authentication and Email Verification

Authentication uses JWT bearer tokens. After logging in, the client sends the token with each request and the API reads the user's id and role from it. Passwords are hashed and never stored in plain text.

Registration requires email verification. After the user fills in the form, the API checks that the email and username are free and that the password meets the rules, then sends a 5-digit code to the user's inbox. The code expires after two minutes, can be requested again only after a 30-second cooldown, and allows five attempts. Codes are held in memory rather than in the database and are never returned in an API response. A "forgot password" flow works in three steps: the user enters their email, confirms the code, then sets a new password. The final step re-validates the code on the server, so the password cannot be changed by calling the endpoint directly.

## Architecture

The solution follows Clean Architecture and is split into four projects. The API project holds the controllers and startup configuration. The Application project contains the DTOs, interfaces, services and FluentValidation validators, which is where the business logic lives. The Core project contains the domain entities and enums: User, Movie, Comment, Reaction and Favorite. The Infrastructure project contains the Entity Framework Core context, the repositories and the migrations. Dependencies point inward, so the domain knows nothing about the database or the web layer.

Controllers stay thin and delegate to services, services talk to repositories through interfaces, and DTOs keep entities from leaking into API responses. Database rules back this up: one reaction and one favorite per user per movie are enforced by unique indexes, and deleting a movie cascades to its comments, reactions and favorites.

## Technology

The API is built with C# and ASP.NET Core on .NET 10, with Entity Framework Core for data access. Locally it runs against SQL Server, while production uses PostgreSQL. Validation is handled by FluentValidation, documentation by Swagger, and transactional emails by the Brevo HTTP API.

## Hosting

The backend is deployed on Render together with a PostgreSQL database. Render's free plan blocks SMTP ports, which is why emails go through Brevo's HTTP API rather than a normal mail server. All secrets, including the connection string, the JWT signing settings and the Brevo credentials, are supplied as environment variables on Render. Locally they are kept in user-secrets so they never end up in source control. The frontend is hosted separately on Netlify, so the API's CORS policy must allow that origin.

## Running Locally

You need the .NET 10 SDK, a database instance, and a Brevo API key if you want to test email. Add your connection string, JWT settings (the key must be at least 32 characters) and Brevo settings to your configuration or user-secrets. Apply the migrations with the Entity Framework tools, then start the API project. Swagger will be available at the /swagger path, where you can try every endpoint.

New accounts start with the User role. To create the first Owner, change that user's role directly in the database. From then on, the Owner can promote other users through the API.
