![Logo](https://i.ibb.co/whNKg9YH/logo.png)
#

XerifeTv CMS is a content management system (CMS) developed for Over-The-Top (OTT) streaming platforms. This project enables efficient organization and management of movies, series, episodes, and TV channels, providing a comprehensive solution for digital content providers.

####
![screenshot](https://i.postimg.cc/xT6Xd7kG/Frame-1-1.png)

####
## 🗺️ System Context Diagram:
![screenshot](https://i.postimg.cc/BbVDG2fw/DIAGRAMAS-TEST-drawio.png)

####
## ✔ Key Features:

- [X]  **Content Management:** Easily register, edit, and organize movies, series, episodes, and TV channels.  
- [X]  **Batch Upload:** Import multiple movies, channels, or full series with episodes at once using spreadsheets (.xlsx).  
- [X]  **TMDB API Integration:** Fetch and autocomplete movie and series data — with automatic import of all episodes — using The Movie Database (TMDB) API.
- [X]  **Supabase Storage Integration:** Store media files (.vtt, images, etc.) securely in a Supabase bucket.  
- [X]  **User Roles & Permissions:** Granular access control for administrators, content managers, and viewers.  
- [X]  **JWT Authentication:** Secure authentication using JSON Web Tokens (JWT).  
- [X]  **Google OAuth2:** Sign in with Google.
- [X]  **Refresh Token Implementation:** Improve session security with access and refresh token flow.  
- [X]  **External Content API:** Public REST API to expose registered content for external apps or clients.  
- [X]  **API Caching:** Improve API response times by caching data from the external content API.  
- [X]  **Swagger Documentation:** All REST endpoints are documented using Swagger/OpenAPI.  
- [X]  **Light/Dark Theme:** Allow users to toggle between light and dark modes for better UX.
 
## 🚀 Technologies:
- C#
- ASP.NET Core
- Razor Pages
- Bootstrap
- JWT Authentication
- Google OAuth2
- MongoDB
- MVC Architecture
- Cache In Memory
- Supabase Storage
- Swagger
- Docker

## 📥 Installing the project

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) version **8.0** or higher  
- **MongoDB** running locally or accessible remotely  
- A **[TMDB account](https://www.themoviedb.org/)** to generate your API key  
- A **[Supabase account](https://supabase.com/)** to manage file storage and access API keys  
- A **[Google Cloud Project](https://console.cloud.google.com/)** with **OAuth 2.0 Client ID** configured for **Web application**  
  - Enable the **Google Identity Services API**  
  - Add authorized JavaScript origins (e.g., `http://localhost:5003`) 

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
            "Key": "",
            "Issuer": "Xerifetvcms",
            "Audience": "Xerifetvcms",
            "ExpirationTimeInMinutes": "",
            "RefreshExpirationTimeInMinutes": ""
        }
    }

#### 6. Configure the Settings for the Supabase in the `appsettings.json` file:
    {
        "Supabase": {
            "Url": "https://example8095.supabase.co",
            "Key": "example4533.exampleee.exammpple"
        }
    }

#### 7. Configure the settings for the Tmdb API integration in the `appsettings.json` file:
    {
        "Tmdb": {
            "Key": "examplekey0examplekey0examplekey"
        }
    }

#### 8. Configure the settings for sending email in the `appsettings.json` file:
    {
        "EmailSettings": {
            "From": "emailexample@gmail.com",
            "Password": "exammpplepassword"
        }
    }

#### 9. Set the base url in the `appsettings.json` file:
    {
        "baseUrl": "http://localhost:5003"
    }

#### 10. Configure Google OAuth2 Client ID in the `appsettings.json` file:
    {
        "OAuth2Google": {
            "ClientId": "your-google-oauth2-client-id.apps.googleusercontent.com"
        }
    }

#### 11. Restore dependencies and compile the project:
    dotnet restore
    dotnet build

#### 12. Start the server:
    dotnet run

#### 13. Access the application in the browser:
    http://localhost:5003


## License

This project is licensed under the [MIT License](LICENSE).
