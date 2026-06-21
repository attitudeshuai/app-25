# 项目25：旅行行李清单共享（TripPacking）

## 请帮我从 0 到 1 实现以下小众项目

### 项目概述
为一次旅行创建行李清单，邀请同行伙伴共同编辑与认领物品（如谁带防晒霜、谁带充电宝），避免重复携带或遗漏。

### 创新点 / 小众定位
把"旅行打包"协作化，支持清单模板、按天行程关联物品、打包完成度与共享状态。

### 目标用户
结伴旅行者、家庭出游、露营爱好者、出差团队

## 项目范围说明
- 本项目为纯后端系统开发，不涉及任何前端页面、UI、CSS/JS 改动。
- 所有功能均通过 RESTful API 对外提供服务，可使用 Postman、curl 或任意 HTTP 客户端进行测试与验收。

## 技术栈（必须严格使用）
- **后端框架**: .NET Core 8.0 (ASP.NET Core Web API)
- **数据库**: MySQL 8.0
- **ORM**: Entity Framework Core 8.0 (Pomelo.EntityFrameworkCore.MySql)
- **认证**: JWT Bearer Token
- **API文档**: Swagger / OpenAPI
- **容器化**: Docker + Docker Compose
- **测试**: xUnit + TestContainers（可选）+ Postman 测试集合

## 项目必须包含的交付物
- **Dockerfile**：多阶段构建，基于上述技术栈。
- **docker-compose.yml**：一键启动应用服务 + MySQL 8.0 + 可选管理工具（如 Adminer）。
- **.gitignore**：针对 .NET Core 的标准忽略配置。
- **README.md**：项目简介、目录说明、快速启动、API 文档入口、测试方式。
- **docs/functional_intro.md**：功能说明、ER 图文字描述、核心用例、业务规则。
- **src/**：完整后端源码（Controller / Service / Repository / Entity / DTO / Mapper / Config 等）。
- **tests/**：单元测试 + 集成测试。
- **postman_collection.json**（或同等测试脚本）：覆盖所有接口的功能测试集合。
- **初始化 SQL / Seed Data**：Docker 启动后自动建表并插入示例数据。

## 数据库设计

### 主要数据表
1. **Users** - 用户表
   - Id（主键）
   - Username（用户名，唯一）
   - Email（邮箱，唯一）
   - PasswordHash（密码哈希）
   - Avatar（头像 URL，可选）
   - CreatedAt / UpdatedAt

2. **Trips** - 旅行
   - OwnerId
   - Title
   - Destination
   - StartDate
   - EndDate
   - Status（Planning / Ongoing / Completed）
   - CreatedAt

3. **TripMembers** - 同行成员
   - TripId
   - UserId
   - Role（Owner / Member）
   - JoinedAt

4. **PackingCategories** - 行李分类
   - TripId
   - Name
   - SortOrder
   - CreatedAt

5. **PackingItems** - 行李物品
   - TripId
   - CategoryId
   - Name
   - Quantity
   - AssignedTo
   - IsPacked
   - IsShared
   - DayNumber
   - CreatedAt

6. **PackingTemplates** - 清单模板
   - Name
   - Category
   - ItemsJson
   - CreatedBy
   - CreatedAt

## 核心功能模块
### 1. 用户认证模块
- 用户注册 / 登录 / JWT 鉴权
- 获取当前登录用户信息

### 2. 旅行管理模块
- 旅行的增删改查（支持分页、搜索、排序）
- 旅行状态/详情/关联操作
- 旅行权限控制（仅所有者或管理员可操作）

### 3. 同行成员管理模块
- 同行成员的增删改查（支持分页、搜索、排序）
- 同行成员状态/详情/关联操作
- 同行成员权限控制（仅所有者或管理员可操作）

### 4. 分类管理模块
- 分类的增删改查（支持分页、搜索、排序）
- 分类状态/详情/关联操作
- 分类权限控制（仅所有者或管理员可操作）

### 5. 物品管理模块
- 物品的增删改查（支持分页、搜索、排序）
- 物品状态/详情/关联操作
- 物品权限控制（仅所有者或管理员可操作）

### 6. 模板管理模块
- 模板的增删改查（支持分页、搜索、排序）
- 模板状态/详情/关联操作
- 模板权限控制（仅所有者或管理员可操作）

### 7. 统计与搜索模块
- 全局搜索与筛选
- 基础数据看板（数量、趋势、排行榜等）
- 导出关键数据（可选）

## API 接口清单
### Auth
- POST /api/auth/register - 用户注册
- POST /api/auth/login - 用户登录
- GET /api/auth/me - 获取当前用户信息
- PUT /api/auth/me - 更新个人信息

### Trips（旅行）
- GET /api/trips - 获取旅行列表（支持分页、搜索、筛选）
- POST /api/trips - 创建旅行
- GET /api/trips/{id} - 获取旅行详情
- PUT /api/trips/{id} - 更新旅行
- DELETE /api/trips/{id} - 删除旅行
- PATCH /api/trips/{id}/status - 修改旅行状态
- GET /api/trips/mine - 获取我发布的/关联的旅行

### TripMembers（同行成员）
- GET /api/tripmembers - 获取同行成员列表（支持分页、搜索、筛选）
- POST /api/tripmembers - 创建同行成员
- GET /api/tripmembers/{id} - 获取同行成员详情
- PUT /api/tripmembers/{id} - 更新同行成员
- DELETE /api/tripmembers/{id} - 删除同行成员
- GET /api/tripmembers/mine - 获取我发布的/关联的同行成员

### PackingCategories（分类）
- GET /api/packingcategories - 获取分类列表（支持分页、搜索、筛选）
- POST /api/packingcategories - 创建分类
- GET /api/packingcategories/{id} - 获取分类详情
- PUT /api/packingcategories/{id} - 更新分类
- DELETE /api/packingcategories/{id} - 删除分类

### PackingItems（物品）
- GET /api/packingitems - 获取物品列表（支持分页、搜索、筛选）
- POST /api/packingitems - 创建物品
- GET /api/packingitems/{id} - 获取物品详情
- PUT /api/packingitems/{id} - 更新物品
- DELETE /api/packingitems/{id} - 删除物品

### PackingTemplates（模板）
- GET /api/packingtemplates - 获取模板列表（支持分页、搜索、筛选）
- POST /api/packingtemplates - 创建模板
- GET /api/packingtemplates/{id} - 获取模板详情
- PUT /api/packingtemplates/{id} - 更新模板
- DELETE /api/packingtemplates/{id} - 删除模板

### Statistics
- GET /api/stats/overview - 总览统计
- GET /api/stats/trend - 趋势统计（按时间范围）

## Docker 配置要求

### Dockerfile（.NET Core）
```dockerfile
# 阶段1：构建
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# 阶段2：运行
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8095
ENV ASPNETCORE_URLS=http://+:8095
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3   CMD curl -f http://localhost:8095/health || exit 1
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

要求：
1. 使用多阶段构建，减少最终镜像体积。
2. 暴露 8095 端口。
3. 添加健康检查接口 `/health`。
4. 通过环境变量读取数据库连接字符串。

### docker-compose.yml 要求
```yaml
version: '3.8'
services:
  app:
    build: .
    container_name: trippacking_app
    ports:
      - "8095:8095"
    environment:
      - DB_HOST=mysql
      - DB_PORT=3306
      - DB_NAME=trippacking
      - DB_USER=app_user
      - DB_PASSWORD=app_pass
    depends_on:
      mysql:
        condition: service_healthy
  mysql:
    image: mysql:8.0
    container_name: trippacking_mysql
    environment:
      - MYSQL_ROOT_PASSWORD=root_pass
      - MYSQL_DATABASE=trippacking
      - MYSQL_USER=app_user
      - MYSQL_PASSWORD=app_pass
    ports:
      - "13320:3306"
    volumes:
      - mysql_data:/var/lib/mysql
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      interval: 10s
      timeout: 5s
      retries: 5
  volumes:
    mysql_data:
```

要求：
1. MySQL 使用 8.0 镜像。
2. 应用服务必须等 MySQL healthy 后再启动。
3. 使用 named volume 持久化数据库数据。
4. 环境变量集中管理，禁止在源码中硬编码密码。

## .gitignore 参考
```text
# .NET / ASP.NET Core
bin/
obj/
*.user
*.suo
*.userosscache
*.sln.docstates
.vs/
*.swp
*.log

# Secrets & local config
appsettings.Development.json
appsettings.Local.json
.env
.env.local

# Test results
TestResults/
coverage/

# IDE
.vscode/
.idea/

# OS
Thumbs.db
.DS_Store
```

## 文档要求

### README.md
至少包含：
1. 项目名称与一句话介绍。
2. 功能亮点（3-5 条）。
3. 技术栈说明。
4. 目录结构说明。
5. 快速启动步骤（克隆 → Docker 启动 → 访问接口）。
6. 测试命令与 Postman 集合导入说明。
7. 贡献与许可（可选）。

### docs/functional_intro.md
至少包含：
1. 业务背景与解决的问题。
2. 用户角色与核心用例。
3. 功能模块详细说明。
4. 数据库 ER 图文字描述（表关系）。
5. 关键业务规则（如状态流转、权限规则、时间计算逻辑）。
6. 接口调用示例（至少 3 个）。

## 运行与测试步骤

1. **克隆并进入项目目录**：
   ```bash
   git clone <repo-url>
   cd TripPacking
   ```

2. **Docker 启动**：
   ```bash
   docker-compose up --build -d
   ```

3. **查看日志**：
   ```bash
   docker-compose logs -f app
   ```

4. **验证服务健康**：
   - .NET：`curl http://localhost:8095/health`
   - Java：`curl http://localhost:8095/actuator/health`

5. **导入并执行 Postman 测试集合**，验证所有接口：
   - 注册 / 登录
   - 各实体的 CRUD
   - 搜索 / 筛选 / 分页
   - 统计接口
   - 权限控制（未登录访问受限资源应返回 401）

6. **执行自动化测试**：
   - .NET：`dotnet test`
   - Java：`./mvnw test` 或 `mvn test`

7. **停止服务**：
   ```bash
   docker-compose down -v
   ```

## 其他质量要求
- 使用 EF Core 操作 MySQL，禁止手写 SQL 进行日常 CRUD（复杂统计可手写）。
- 代码分层清晰，遵循 RESTful API 设计规范。
- 关键代码必须有中文注释，说明业务意图。
- 统一的异常处理与参数校验（.NET FluentValidation / Spring Validation）。
- 使用 JWT 保护敏感接口，未携带 Token 返回 401。
- 数据库连接字符串通过环境变量注入，支持 Docker 内外运行。
- 提供 Seed Data，容器启动后至少有 5-10 条示例数据可用于测试。
- 接口返回统一包装格式（code / message / data）。
- 日志使用框架原生日志（.NET ILogger / SLF4J），记录关键操作与异常。
- 项目必须是小众生活/工作场景，禁止做成通用商城、OA、CMS、ERP。
