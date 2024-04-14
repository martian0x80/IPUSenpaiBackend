# IPUSenpaiBackend
This is the backend for the IPUSenpai project. It is a RESTful API that provides endpoints for the IPUSenpai frontend.

The frontend for this project can be found [here](https://devel.ipusenpai.in/).

## Brief Overview
- The backend is built using ASP.NET Core and ~Entity Framework Core~ Dapper. It is hosted on Azure and uses Azure Postgresql Database for data storage. (Will be moved to my VPS after I run out of Azure Student Sponsorship balance)
- The API uses Redis for caching. This is to reduce the number of database queries and improve performance.
- The API uses Brotli and Gzip compression to reduce the size of the response body.

Here's a peek of the current student dashboard:
![Student Dashboard](https://github.com/martian0x80/IPUSenpaiBackend/assets/26498920/2712d001-eae1-4b83-a2bf-4879c76fe64c)

## Like My Work?
- If you like my work, you can star the repository.

> [!NOTE]  
> This project is still in development and is not yet ready for production.
> I will provide an OpenAPI documentation for the API once it is ready for use.

## Issues
- Report issues [here](https://github.com/martian0x80/IPUSenpaiBackend/issues).
- Don't report any issues if they are already known or listed. Just simply react.
