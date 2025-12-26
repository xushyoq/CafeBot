# CafeBot Development Makefile

.PHONY: help build run test clean docker-build docker-run docker-stop migrate seed

# Default target
help:
	@echo "Available commands:"
	@echo "  build         - Build the application"
	@echo "  run           - Run the application"
	@echo "  test          - Run unit tests"
	@echo "  clean         - Clean build artifacts"
	@echo "  docker-build  - Build Docker image"
	@echo "  docker-run    - Run with Docker Compose"
	@echo "  docker-stop   - Stop Docker containers"
	@echo "  migrate       - Run database migrations"
	@echo "  seed          - Seed database with test data"
	@echo "  format        - Format code"
	@echo "  lint          - Run code analysis"

# Build the application
build:
	dotnet build CafeBot.sln

# Run the application
run:
	cd CafeBot.TelegramBot && dotnet run

# Run unit tests
test:
	dotnet test CafeBot.Tests --verbosity normal

# Clean build artifacts
clean:
	dotnet clean CafeBot.sln
	rm -rf CafeBot.*/bin CafeBot.*/obj
	rm -rf CafeBot.Tests/TestResults

# Docker commands
docker-build:
	docker build -t cafebot .

docker-run:
	docker-compose up -d

docker-stop:
	docker-compose down

# Database operations
migrate:
	cd CafeBot.TelegramBot && dotnet ef database update

seed:
	cd CafeBot.TelegramBot && dotnet run --seed

# Code quality
format:
	dotnet format CafeBot.sln

lint:
	dotnet build CafeBot.sln --no-restore /p:RunCodeAnalysis=true

# Development setup
setup:
	@echo "Setting up development environment..."
	dotnet restore CafeBot.sln
	cp CafeBot.TelegramBot/appsettings.json.example CafeBot.TelegramBot/appsettings.Development.json
	@echo "Please update CafeBot.TelegramBot/appsettings.Development.json with your configuration"
	@echo "Setup complete!"

# Full development cycle
dev: clean build test run

# Production build
prod-build:
	dotnet publish CafeBot.TelegramBot -c Release -o ./publish --no-build

# Generate documentation (future)
docs:
	@echo "Documentation generation not implemented yet"
