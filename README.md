![Logo](https://i.ibb.co/whNKg9YH/logo.png)
#

XerifeTv CMS is a content management system (CMS) developed for Over-The-Top (OTT) streaming platforms. This project enables efficient organization and management of movies, series, episodes, and TV channels, providing a comprehensive solution for digital content providers.

####
![screenshot](https://i.ibb.co/tp5h40F5/Screenshot-from-2025-03-12-18-53-08.png)

####
## ✔ Key Features:
- [X]  **Content Management:** Easily register, edit, and organize movies, series, episodes, and TV channels.  
- [X]  **User Roles & Permissions:** Granular access control for administrators, content managers, and visitors.  
- [X]  **JWT Authentication:** Secure authentication system based on JSON Web Tokens.  
- [X]  **External Content API:** Provide a REST API to securely expose registered content for consumption by external applications and websites.  
- [X]  **API Caching for External Content:** Optimize performance by caching responses from the External Content API. 
- [X]  **Supabase Storage Integration:** Store files (.vtt, images, etc...) in a storage bucket.  
- [X]  **Swagger Documentation:** REST API routes documented with Swagger.  
- [X]  **Light/Dark Theme:** Enable users to switch between light and dark themes.
- [X]  **Refresh Token Implementation:** Enhance authentication security by adding refresh token support.  
## 🚀 Technologies:
- C#
- ASP.NET Core
- Razor Pages
- Bootstrap
- JWT Authentication
- MongoDB
- MVC Architecture
- Cache In Memory
- Supabase Storage
- Swagger
- Docker

## 📥 Installing the project

### Prerequisites

- **.NET SDK** version 6.0 or higher
- **MongoDB** installed and running locally or on a remote service

### Setup Steps


#### 1. Clone the repository:
    git clone https://github.com/GilbertSilvaa/XerifeTv.CMS.git


#### 2. Navigate to the project directory:
    cd XerifeTv.CMS

#### 3. Configure the MongoDB connection string in the `appsettings.json` file:
    {
      "MongoDBConfig": {
            "ConnectionString": "mongodb://localhost:00000",
            "DatabaseName": "xerifetv_content"
        }
    }

#### 4. Configure Hash Salt for password encryption in `appsettings.json` file:
    {
        "Hash": {
            "Salt": "HASHSALT0000HASH5555HASH0000"
        }
    }

#### 5. Configure the Settings for the JWT token in the `appsettings.json` file:
    {
        "Jwt": {
            "Key": "jwtkey555jwtksajdkl387432sdasda347823974923ns3676",
            "Issuer": "Xerifetvcms",
            "Audience": "Xerifetvcms"
        }
    }

#### 6. Configure the Settings for the Supabase in the `appsettings.json` file:
    {
        "Supabase": {
            "Url": "https://example8095.supabase.co",
            "Key": "example4533.exampleee.exammpple"
        }
    }

#### 7. Restore dependencies and compile the project:
    dotnet restore
    dotnet build


#### 8. Start the server:
    dotnet run


#### 9. Access the application in the browser:
    http://localhost:5000

## License

This project is licensed under the [MIT License](LICENSE).
