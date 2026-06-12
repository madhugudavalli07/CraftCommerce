# CraftCommerce Architecture

## Application Type

Microservices Architecture

## Services

### User Service
Responsibilities:
- Registration
- Login
- JWT Authentication

### Product Service
Responsibilities:
- Product Management
- Categories
- Inventory

### Cart Service
Responsibilities:
- Add To Cart
- Update Cart
- Remove Cart Items

### Order Service
Responsibilities:
- Place Order
- Order History
- Order Tracking

## High Level Flow

Angular UI

↓

API Gateway (Future)

↓

User Service
Product Service
Cart Service
Order Service

↓

SQL Server Databases
