# IPUSenpaiBackend
This is the backend for the IPUSenpai project. It is a RESTful API that provides endpoints for the frontend to interact with the database.

The frontend for this project can be found [here](https://ipu-senpai.vercel.app/).

## Brief Overview
- The backend is built using ASP.NET Core and Entity Framework Core. It is hosted on Azure and uses Azure Postgresql Database for data storage.
- The API uses Redis for caching. This is to reduce the number of database queries and improve performance.
- The API uses Brotli and Gzip compression to reduce the size of the response body.

> [!NOTE]  
> This project is still in development and is not yet ready for production.
> I will provide an OpenAPI documentation for the API once it is ready for use.
