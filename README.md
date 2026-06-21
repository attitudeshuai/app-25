# TripPacking - 旅行行李清单共享系统

一款帮助结伴旅行者共同管理行李清单的协作工具

## Features

- **协作打包** — 多人实时协作编辑同一份行李清单，物品可分配给指定成员
- **清单模板** — 内置和自定义打包模板，一键应用快速创建行李清单
- **按天关联** — 物品可关联旅行天数，分阶段整理行李
- **打包进度** — 实时统计已打包/未打包数量，可视化展示打包进度
- **多端API** — RESTful API 设计，支持 Web、小程序、移动端等多端接入

## Tech Stack

- **.NET 8** — 最新 LTS 版本框架
- **ASP.NET Core Web API** — Web API 服务框架
- **EF Core 8** — ORM 数据访问
- **MySQL 8** — 关系型数据库
- **Pomelo.EntityFrameworkCore.MySql** — MySQL EF Core 驱动
- **JWT** — 基于 Token 的身份认证
- **AutoMapper** — 对象映射
- **Swagger / OpenAPI** — API 文档与调试
- **Docker** — 容器化部署

## Directory Structure

```
TripPacking/
├── src/
│   └── TripPacking/
│       ├── Controllers/       # API 控制器层
│       ├── Services/          # 业务逻辑层
│       ├── Repositories/      # 数据访问层
│       ├── Entities/          # 数据库实体
│       ├── DTOs/              # 数据传输对象
│       ├── Mappers/           # AutoMapper 映射配置
│       ├── Data/              # DbContext 与种子数据
│       ├── Config/            # 配置类 (JwtSettings 等)
│       ├── Middleware/        # 自定义中间件 (异常处理、JWT)
│       ├── Program.cs         # 应用入口
│       ├── appsettings.json   # 配置文件
│       └── TripPacking.csproj # 项目文件
├── tests/
│   └── TripPacking.Tests/     # 单元测试项目
├── docs/                      # 项目文档
├── docker-compose.yml         # Docker 编排文件
├── Dockerfile                 # Docker 镜像构建
├── TripPacking.sln            # 解决方案文件
└── README.md
```

## Quick Start

### Prerequisites

- Docker & Docker Compose

### Run with Docker

```bash
docker-compose up --build -d
```

Services will be available at:

- **API**: http://localhost:8095
- **Swagger UI**: http://localhost:8095/swagger
- **MySQL**: localhost:13320

### Health Check

```bash
curl http://localhost:8095/health
```

Expected response:

```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## Testing

### Run Unit Tests

```bash
dotnet test
```

### Postman Collection

Import `postman_collection.json` into Postman to access all API endpoints with pre-configured requests.

The collection includes:
- Auth (register, login, get/update profile)
- Trips (CRUD, status update, my trips)
- TripMembers (manage trip members and roles)
- PackingCategories (item categories per trip)
- PackingItems (individual packing items)
- PackingTemplates (shared and personal templates)
- Stats (overview statistics and trend data)

## API Endpoints

### Auth (`/api/auth`)
| Method | Endpoint     | Description        | Auth |
|--------|--------------|--------------------|------|
| POST   | /register    | Register new user  | No   |
| POST   | /login       | Login and get token| No   |
| GET    | /me          | Get current user   | Yes  |
| PUT    | /me          | Update current user| Yes  |

### Trips (`/api/trips`)
| Method | Endpoint        | Description          | Auth |
|--------|-----------------|----------------------|------|
| GET    | /               | List trips (paged)   | Yes  |
| POST   | /               | Create new trip      | Yes  |
| GET    | /mine           | List my trips        | Yes  |
| GET    | /{id}           | Get trip by id       | Yes  |
| PUT    | /{id}           | Update trip          | Yes  |
| DELETE | /{id}           | Delete trip          | Yes  |
| PATCH  | /{id}/status    | Update trip status   | Yes  |

### TripMembers (`/api/tripmembers`)
| Method | Endpoint     | Description              | Auth |
|--------|--------------|--------------------------|------|
| GET    | /            | List members (paged)     | Yes  |
| POST   | /            | Add member to trip       | Yes  |
| GET    | /mine        | List my memberships      | Yes  |
| GET    | /{id}        | Get member by id         | Yes  |
| PUT    | /{id}        | Update member role       | Yes  |
| DELETE | /{id}        | Remove member from trip  | Yes  |

### PackingCategories (`/api/packingcategories`)
| Method | Endpoint     | Description            | Auth |
|--------|--------------|------------------------|------|
| GET    | /            | List categories        | Yes  |
| POST   | /            | Create category        | Yes  |
| GET    | /{id}        | Get category by id     | Yes  |
| PUT    | /{id}        | Update category        | Yes  |
| DELETE | /{id}        | Delete category        | Yes  |

### PackingItems (`/api/packingitems`)
| Method | Endpoint     | Description         | Auth |
|--------|--------------|---------------------|------|
| GET    | /            | List items          | Yes  |
| POST   | /            | Create item         | Yes  |
| GET    | /{id}        | Get item by id      | Yes  |
| PUT    | /{id}        | Update item         | Yes  |
| DELETE | /{id}        | Delete item         | Yes  |

### PackingTemplates (`/api/packingtemplates`)
| Method | Endpoint     | Description         | Auth |
|--------|--------------|---------------------|------|
| GET    | /            | List templates      | No   |
| POST   | /            | Create template     | Yes  |
| GET    | /{id}        | Get template by id  | No   |
| PUT    | /{id}        | Update template     | Yes  |
| DELETE | /{id}        | Delete template     | Yes  |

### Stats (`/api/stats`)
| Method | Endpoint     | Description           | Auth |
|--------|--------------|-----------------------|------|
| GET    | /overview    | Get overview stats    | Yes  |
| GET    | /trend       | Get trend statistics  | Yes  |

## Ports

| Service | Port  | Description |
|---------|-------|-------------|
| API     | 8095  | ASP.NET Core Web API |
| MySQL   | 13320 | MySQL 8.0 database (maps to internal 3306) |
