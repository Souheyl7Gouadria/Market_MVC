🎯 Overview
Market is a full-featured, production-ready e-commerce web application built with ASP.NET Core MVC and .NET 8.
Designed with clean architecture principles and enterprise-grade patterns, the platform demonstrates modern web development best practices while delivering a complete online book store experience.

✨ Key Features
🛒 **Customer Experience**
•	Product Catalog - Browse products with category filtering and detailed product pages
•	Multi-Image Support - Products with multiple image galleries
•	Smart Shopping Cart - Session-based cart with real-time item count display via View Components
•	Tiered Pricing - Dynamic pricing based on quantity (1-50, 51-100, 100+)
•	Secure Checkout - Streamlined order summary and checkout flow
💳 **Payment Processing**
•	Stripe Integration - Secure payment processing with Stripe Checkout Sessions
•	Delayed Payment for Companies - B2B customers can place orders with Net 30 payment terms
•	Refund Support - Full refund processing through Stripe API for cancelled orders
•	Payment Confirmation - Real-time payment status verification
👤 **User Management**
•	ASP.NET Core Identity - Robust authentication and authorization (managed with ASP.Net)
•	Role-Based Access Control - Four distinct roles: Customer, Company, Employee, Admin
•	Facebook OAuth - Social login integration for streamlined registration
•	Account Lock/Unlock - Admin-controlled user access management
•	Role Management - Dynamic role assignment with company association
📦 **Order Management**
•	Complete Order Lifecycle - Pending → Approved → Processing → Shipped
•	Order Tracking - Carrier and tracking number support
•	Status Filtering - Filter orders by status (All, Pending, In Process, Completed, Approved)
•	Order Details - Comprehensive order information with customer and product details
🔧 **Admin Dashboard**
•	Product Management - Full CRUD with multi-image upload and TinyMCE rich text editor
•	Category Management - Organize products with display ordering
•	Company Management - B2B customer company profiles
•	User Administration - Complete user lifecycle management with role assignment

🏗️ Architecture & Design Patterns
**Clean Architecture**
The solution follows a layered architecture with clear separation of concerns:
📁 Market Solution
├── 📂 MarketWeb              # Presentation Layer (MVC Controllers, Views, ViewComponents)
├── 📂 Market.DataAccess      # Data Access Layer (EF Core, Repositories, Migrations)
├── 📂 Market.Models          # Domain Layer (Entities, ViewModels)
└── 📂 Market.Utility         # Cross-Cutting Concerns (Constants, Settings, Email)

**Design Patterns Implemented**

#### 🔄 Repository Pattern
Generic `IRepository<T>` providing a consistent data access abstraction with expression-based filtering and eager loading support.

#### 🎯 Unit of Work Pattern
Centralized transaction management through `IUnitOfWork`, ensuring atomic database operations across multiple repositories with a single `Save()` method.

#### 💉 Dependency Injection
All services registered via ASP.NET Core's built-in IoC container with scoped lifetime management.

#### 🧩 View Components
Reusable, self-contained UI components (e.g., `CartItemViewComponent`) with their own logic and views.

#### 📋 ViewModel Pattern
Strongly-typed view models (`ProductVM`, `OrderVM`, `RoleManagementVM`) for complex view data binding.

#### ⚙️ Options Pattern
Typed configuration binding for external services (Stripe settings, Facebook OAuth).

#### 🏭 Database Initializer Pattern
Automatic database seeding and migration via `IDbInitializer` on application startup.


🛠️ Technology Stack
**Backend**
•	.NET 8 - Latest LTS framework
•	ASP.NET Core MVC - Web framework with Areas support
•	Entity Framework Core 8 - ORM with Code-First migrations
•	ASP.NET Core Identity - Authentication & Authorization
•	SQL Server - Relational database

**Frontend**
•	Razor Views - Server-side rendering
•	Bootstrap 5 - Responsive UI framework
•	DataTables - Interactive data tables with sorting, searching, and pagination
•	TinyMCE - Rich text editor for product descriptions
•	Toastr - Toast notifications
•	SweetAlert2 - Beautiful alert dialogs

**Integrations**
•	Stripe - Payment processing (Checkout Sessions, Refunds)
•	Facebook OAuth - Social authentication

🔐 **Security Features**
•	Role-Based Authorization - Attribute-based access control on controllers and actions
•	HTTPS Enforcement - Secure communication in production
•	Account Lockout - Protection against brute force attacks

🚀 **Getting Started**
Prerequisites
•	.NET 8 SDK
•	SQL Server (LocalDB or full instance)
•	Stripe Account (for payment processing)
•	Facebook Developer App (optional, for social login)
Configuration
1.	Clone the repository
2.	Update appsettings.json with your connection string and API keys
3.	Run the application - migrations and seed data are applied automatically via DbInitializer

📁 **Project Structure Highlights**
   MarketWeb/
├── Areas/
│   ├── Admin/           # Admin controllers & views (Product, Category, Company, User, Order)
│   ├── Customer/        # Customer controllers & views (Home, Cart)
│   └── Identity/        # Scaffolded Identity pages (Login, Register, External Login)
├── ViewComponents/      # Reusable UI components
├── Views/Shared/        # Layouts, partials, notifications
└── wwwroot/             # Static assets (CSS, JS, images)

Market.DataAccess/
├── Data/                # DbContext with seed data
├── DbInitializer/       # Database initialization & migration
├── Repository/          # Generic and specific repositories
└── Migrations/          # EF Core migrations

Market.Models/
├── Domain entities      # Product, Category, Order, etc.
└── ViewModel/           # ProductVM, CartItemVM, OrderVM, RoleManagementVM


🙏 Made with ❤️ by Souheyl Gouadria
