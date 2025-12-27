# 🍽️ CafeBot - Telegram Bot for Cafe Management

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue.svg)](https://www.postgresql.org/)
[![Telegram Bot API](https://img.shields.io/badge/Telegram%20Bot%20API-7.0+-blue.svg)](https://core.telegram.org/bots/api)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

CafeBot is a modern Telegram bot for automating cafe management processes, including order taking, menu management, staff management, and analytics.

## ✨ Features

### 👥 Staff Management
- Employee registration and authentication
- Access control (Admin/Waiter roles)
- Staff activity tracking
- Employee performance statistics

### 📋 Order Management
- Online table reservations
- Order creation and management
- Order status tracking
- Order item management
- Payment processing

### 🍽️ Menu Management
- Product categories
- Add/edit/delete menu items
- Price and description management
- Dish photos
- Product availability control

### 🏠 Room Management
- Room and table registration
- Capacity management
- Room booking
- Occupancy tracking

### 📊 Analytics and Reports
- Sales statistics
- Staff analytics
- Popular dish reports
- Financial reports

## 🏗️ Architecture

The project is built on **Clean Architecture** principles and divided into the following layers:

```
CafeBot/
├── Core/                    # Business logic and domain entities
│   ├── Entities/           # Domain models
│   ├── Enums/             # Enumerations
│   └── Interfaces/        # Repository contracts
├── Application/           # Application services
│   └── Services/         # Business logic services
├── Infrastructure/        # External dependencies
│   ├── Data/             # DB configuration and context
│   ├── Repositories/     # Repository implementations
│   └── Migrations/       # DB migrations
├── TelegramBot/          # Telegram Bot interface
│   ├── Bot/             # Telegram interaction logic
│   ├── Handlers/        # Command and callback handlers
│   ├── Keyboards/       # Bot keyboards
│   └── States/          # User state management
└── Tests/               # Unit and integration tests
```

## 🛠️ Technologies

- **Backend**: .NET 8.0, C# 12
- **Database**: PostgreSQL 15+
- **ORM**: Entity Framework Core 8.0
- **Bot Framework**: Telegram.Bot 19.0+
- **Testing**: xUnit, FluentAssertions, Moq
- **Logging**: Microsoft.Extensions.Logging
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- Telegram Bot account (get token from [@BotFather](https://t.me/botfather))
- [Docker](https://www.docker.com/) (optional, for containerization)

## 🔒 Security

### ⚠️ Important!

**Never commit to repository:**
- Real Telegram bot tokens
- Database passwords
- API keys
- Private keys

### Files excluded from Git:
- `appsettings.json` (contains local settings)
- `.env` (environment variables)
- Any files with extensions: `.key`, `.pem`, `.p12`, `.pfx`

### Using environment variables:
```bash
# Copy example
cp env.example .env

# Edit with secure data
nano .env
```

## 🚀 Installation and Setup

### 1. Clone the repository

```bash
git clone https://github.com/yourusername/cafebot.git
cd cafebot
```

### 2. Database setup

```bash
# Create PostgreSQL database
createdb cafebot

# Create user (optional)
createuser cafebotuser --pwprompt --encrypted
# Password: CafeBot2025!Strong

# Grant permissions
GRANT ALL PRIVILEGES ON DATABASE cafebot TO cafebotuser;
```

### 3. Application configuration

⚠️ **NEVER commit real tokens and passwords to the repository!**

#### For local development:
Create `.env` file in the project root:

```bash
# Copy example
cp env.example .env

# Edit with your data
nano .env
```

`.env` content:
```env
# Get from @BotFather in Telegram
TELEGRAM_BOT_TOKEN=your_bot_token_here

# PostgreSQL settings
DB_HOST=localhost
DB_PORT=5432
DB_NAME=cafebot
DB_USER=cafebotuser
DB_PASSWORD=your_secure_password
```

#### For production:
Use environment variables of the deployment platform (Railway, Render, etc.):

```env
TELEGRAM_BOT_TOKEN=your_bot_token
ConnectionStrings__DefaultConnection=postgresql://user:pass@host:port/db
```

### 4. Run the application

#### Option 1: Local run
```bash
# From CafeBot.TelegramBot directory
cd CafeBot.TelegramBot
dotnet run
```

#### Option 2: Using Makefile
```bash
# Quick setup
make setup

# Run
make run
```

#### Option 3: Docker
```bash
# Build and run
docker-compose up --build

# Or using Makefile
make docker-run
```

### 5. Data initialization

On first run, the application will automatically create the DB structure and populate with test data.

## 🤖 Usage

### Bot commands

| Command | Description |
|---------|-------------|
| `/start` | Start bot and user registration |
| `/help` | Commands help |
| `🔧 Admin panel` | Access to administrative functions |

### Admin panel

#### Staff management
- Add new employees
- View staff list
- Manage roles and status
- Work statistics

#### Product management
- Add/edit menu items
- Manage categories
- Set prices and descriptions
- Control product availability

#### Room management
- Register new rooms
- Configure capacity
- Manage room status

#### Order management
- View active orders
- Change order status
- Manage order items
- Process payments

## 🧪 Testing

### Run unit tests

```bash
# From project root directory
dotnet test CafeBot.Tests

# Or using Makefile
make test
```

### Run with code coverage

```bash
dotnet test CafeBot.Tests --collect:"XPlat Code Coverage"
```

### Test structure

```
Tests/
├── Services/           # Business logic tests
├── Handlers/          # Telegram handlers tests
├── Infrastructure/    # Repository tests
└── Integration/       # Integration tests
```

## 📊 Monitoring

### Logs

The application provides detailed logging of all operations:
- User action info messages
- Errors and exceptions
- SQL queries (in development mode)

### Metrics

Future plans include:
- Number of active orders
- Bot response time
- Feature usage statistics

## 🔧 Development

### Code conventions

- Use PascalCase for class and method names
- camelCase for parameters and local variables
- Detailed XML comments for public APIs
- Maximum line length: 120 characters

### Adding new features

1. Create issue in issue tracker
2. Write tests for new functionality
3. Implement the feature
4. Ensure all tests pass
5. Update documentation

### Database operations

```bash
# Create new migration
dotnet ef migrations add MigrationName -p CafeBot.Infrastructure -s CafeBot.TelegramBot

# Apply migrations
dotnet ef database update -p CafeBot.Infrastructure -s CafeBot.TelegramBot
```

## 🐳 Docker

```bash
# Build image
docker build -t cafebot .

# Run with PostgreSQL
docker-compose up
```

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### PR requirements
- All tests must pass
- Code must be covered by tests
- Follow project code style
- Update documentation if necessary

## 📝 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Xush** - *Main developer* - [GitHub](https://github.com/xush)

## 🙏 Acknowledgments

- Telegram Bot API for excellent platform
- .NET community for fantastic tools
- All project contributors

## 📞 Support

If you have questions or issues:
1. Check [Issues](https://github.com/yourusername/cafebot/issues)
2. Create new issue with detailed problem description
3. Contact the developer

## 📚 Additional documentation

- [Contributing Guide](CONTRIBUTING.md) - How to contribute to the project
- [Changelog](CHANGELOG.md) - Change history
- [Makefile](Makefile) - Development commands
- [Docker Setup](docker-compose.yml) - Docker configuration

## 🏗️ Project architecture

```
CafeBot/
├── Core/                    # Business logic and domain entities
│   ├── Entities/           # Domain models
│   ├── Enums/             # Enumerations
│   └── Interfaces/        # Repository contracts
├── Application/           # Application services
│   └── Services/         # Business logic services
├── Infrastructure/        # External dependencies
│   ├── Data/             # DB configuration and context
│   ├── Repositories/     # Repository implementations
│   └── Migrations/       # DB migrations
├── TelegramBot/          # Telegram Bot interface
│   ├── Bot/             # Telegram interaction logic
│   ├── Handlers/        # Command and callback handlers
│   ├── Keyboards/       # Bot keyboards
│   └── States/          # User state management
└── Tests/               # Unit and integration tests
```

---

⭐ If you find this project helpful, give it a star on GitHub!