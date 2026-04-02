# InvestmentTracker v1 — Investment & Portfolio Platform

## 1. Descripción General

**InvestmentTracker v1** es una plataforma de inversiones basada en arquitectura de microservicios.
El sistema permite registrar usuarios, operar activos financieros (acciones, bonos, oro, etc.), mantener el historial de transacciones y calcular el estado actual de la cartera de inversiones en tiempo real.

El objetivo principal del proyecto es simular una arquitectura similar a la utilizada por plataformas fintech o brokers, priorizando:

* Escalabilidad
* Desacoplamiento entre servicios
* Consistencia eventual
* Comunicación asíncrona
* Buenas prácticas de arquitectura (Clean Architecture + DDD + Event-Driven)

---

## 2. Estructura del Repositorio

```
InvestmentTracker/
├──ApiGateways/
├──BuildingBlocks/
├── src/
│   ├── Services/
│   │   ├── Users/
│   │   ├── Holdings/
│   │   ├── Transactions/
│   │   └── MarketData/
│   ├── ApiGateways/
│   └── BuildingBlocks/
├── docker-compose.yml
└── InvestmentTracker.sln
```

### Descripción

| Carpeta        | Responsabilidad                                             |
| -------------- | ----------------------------------------------------------- |
| Services       | Microservicios independientes del sistema                   |
| ApiGateways    | Punto de entrada unificado (futuro gateway)                 |
| BuildingBlocks | Código compartido (EventBus, logging, contratos de eventos) |
| docker-compose | Orquestación local de toda la plataforma                    |

---

## 3. Microservicios

| Servicio         | Responsabilidad                                     |
| ---------------- | --------------------------------------------------- |
| Users.API        | Gestión de usuarios, perfiles y KYC                 |
| Transactions.API | Registro de compras, ventas y depósitos             |
| Holdings.API     | Estado actual de la cartera (posiciones activas)    |
| MarketData.API   | Obtención de precios de mercado desde APIs externas |

Arquitecturalmente el sistema se divide en:

* **Identidad:** Users
* **Historial:** Transactions
* **Estado Actual:** Holdings
* **Datos Externos:** MarketData

---

## 4. Arquitectura Interna de cada Microservicio

Cada servicio sigue **Clean Architecture**.

```
Users
 ├── Users.Domain
 ├── Users.Application
 ├── Users.Infrastructure
 └── Users.API
```

### Capa de Dominio

Contiene entidades y reglas de negocio puras.

Ejemplo:

* User
* Validaciones KYC
* Reglas del negocio

No depende de frameworks ni de base de datos.

---

### Capa de Aplicación

Orquesta los casos de uso.

Contiene:

* DTOs
* Interfaces
* Servicios de aplicación

Ejemplo:
`IUserRepository`

Define *qué* se puede hacer con los usuarios, no *cómo*.

---

### Capa de Infraestructura

Implementa los detalles técnicos.

Contiene:

* Entity Framework Core
* Repositorios
* Persistencia en SQL Server

Ejemplo:
`UserRepository : IUserRepository`

---

### Capa API

Expone endpoints HTTP.

Responsabilidades:

* Recibir requests
* Llamar a Application
* Publicar eventos

No contiene lógica de negocio.

---

## 5. Patrón Repository

El DbContext no se utiliza directamente en los Controllers.

Motivos:

* Testeabilidad
* Desacoplamiento
* Sustituibilidad (ej: cambiar SQL Server por Mongo)

Registro en DI:

```
builder.Services.AddScoped<IUserRepository, UserRepository>();
```

### ¿Por qué Scoped?

Se crea una instancia por request HTTP, lo que:

* Evita fugas de conexión
* Mantiene el ciclo de vida correcto del DbContext
* Es la forma recomendada para EF Core

---

## 6. Modelo de Inversiones (Portfolio)

El sistema no usa columnas por activo (ej: columna Oro, columna Acciones).

Usa un modelo **escalable de Posiciones**:

### Entidades principales

**Account / Wallet**

* Pertenece a un User
* Contiene efectivo disponible

**Asset**

* Representa un instrumento financiero
* Ej: AAPL, AL30, GOLD

**Position**

* Relación entre cuenta y activo
* Ej: Usuario posee 10 AAPL

**Transaction**

* Historial de operaciones (buy/sell/deposit)

Esto permite agregar nuevos activos sin modificar la base de datos.

---

## 7. Comunicación entre Microservicios

### Principio clave

Los microservicios **NO se referencian entre sí a nivel de código (.csproj).**

Se comunican mediante:

| Tipo                   | Uso                               |
| ---------------------- | --------------------------------- |
| RabbitMQ + MassTransit | Eventos de negocio                |
| HTTP/gRPC              | Consultas puntuales (ej: precios) |

Ejemplo:

* Transactions → Holdings → (eventos)
* Holdings → MarketData → (HTTP)

---

## 8. Event-Driven Architecture

Cuando ocurre un evento importante se publica un mensaje:

Ejemplo:
`UserCreatedEvent`

Posibles consumidores:

* Portfolio crea la billetera automáticamente
* Notifications envía email
* Analytics registra métricas

El servicio emisor **no conoce a los consumidores**.

Esto es desacoplamiento real.

---

## 9. El Problema del Dual Write

Caso:

Al comprar un activo:

1. Guardar transacción en DB
2. Publicar `TransactionCreatedEvent`

Escenarios posibles:

| DB   | RabbitMQ | Resultado                     |
| ---- | -------- | ----------------------------- |
| OK   | OK       | Correcto                      |
| OK   | FAIL     | Holdings nunca se actualiza ❌ |
| FAIL | OK       | Evento fantasma ❌             |

**Dual Write Problem**.

---

## 10. Patrón Transactional Outbox

Solución: guardar el evento en la misma transacción que la base de datos.

### Flujo

1. Se guarda la transacción
2. Se guarda el evento en tabla `OutboxMessages` (estado Pendiente)
3. Un worker lo publica a RabbitMQ
4. Se marca como procesado

Tabla:

| Id | Type | Content | OccurredOnUtc | ProcessedOnUtc |

Importante:

**No se modifica el evento de negocio.**
Solo se actualiza el registro técnico del outbox.

Es equivalente a:

> La carta no cambia. Solo se sella el sobre.

---

## 11. OutboxPublisherService

Es un `BackgroundService` de .NET.

Características:

* Corre automáticamente al iniciar la API
* Vive toda la vida de la aplicación
* Publica eventos pendientes

Problema:
`BackgroundService` es Singleton
`DbContext` es Scoped

Solución:
`IServiceScopeFactory`

Permite crear un scope manual dentro de `ExecuteAsync`.

El worker:

1. Busca eventos no publicados
2. Los publica
3. Marca `ProcessedOnUtc`

---

## 12. Consistencia Eventual

El sistema no es inmediatamente consistente.

El flujo real:

Usuario compra → Transactions guarda → Evento → Holdings procesa → Portfolio actualizado

Puede haber milisegundos o segundos de diferencia.

Esto es normal en sistemas distribuidos.

---

## 13. Protección contra mensajes desordenados

Cada evento posee:

* Timestamp
* o Version

Si un consumer recibe un evento más viejo que el último procesado, lo ignora.

Previene:

* sobrescrituras incorrectas
* estados inconsistentes

---

## 14. Reglas de Arquitectura

### Flujo interno

```
Controller → Application Service → Repository → DbContext
                                   ↘ EventBus
```

### El Service NO debe manejar:

* SqlException
* DbUpdateException
* TimeoutException

Solo:

* reglas de negocio
* validaciones
* estados del dominio

Las excepciones técnicas pertenecen a infraestructura/pipeline HTTP.

---

## 15. Responsabilidades globales

| Capacidad                     | Responsable            |
| ----------------------------- | ---------------------- |
| Registro de usuarios          | Users.API              |
| Comunicación asíncrona        | RabbitMQ + MassTransit |
| Creación automática de cuenta | Consumer en Portfolio  |
| Lógica de inversión           | Holdings/Portfolio     |
| Consulta de estado            | Holdings.API           |



/////////////////////////////////////////////////////////////////////////////////////////////


# Wallet App v1

**Wallet App v1** es una plataforma de inversiones basada en microservicios que simula la arquitectura interna de un broker/fintech.

El sistema permite registrar usuarios, operar activos financieros y mantener un portfolio actualizado mediante eventos asíncronos.

> Este proyecto fue desarrollado con fines educativos para practicar arquitectura distribuida real: Clean Architecture, Event-Driven Architecture y consistencia eventual.

---

## ✨ Características

* Registro de usuarios con KYC
* Registro de compras y ventas de activos
* Historial de transacciones
* Portfolio actualizado automáticamente
* Consumo de precios de mercado externos
* Comunicación entre servicios mediante eventos
* Protección contra inconsistencias de datos (Transactional Outbox)

---

## 🧠 Arquitectura

El sistema sigue una arquitectura de **microservicios desacoplados**.

Servicios principales:

| Servicio     | Función                     |
| ------------ | --------------------------- |
| Users        | Gestión de usuarios         |
| Transactions | Registro de operaciones     |
| Holdings     | Estado actual de la cartera |
| MarketData   | Precios de mercado externos |

Comunicación:

* Eventos → RabbitMQ + MassTransit
* Consultas → HTTP

Cada microservicio implementa **Clean Architecture**:

```
API → Application → Domain
           ↓
     Infrastructure
```

---

## 🧩 Tecnologías

* .NET 8
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* RabbitMQ
* MassTransit
* Docker & Docker Compose

---

## 📨 Event-Driven + Outbox Pattern

El sistema utiliza el patrón **Transactional Outbox** para evitar inconsistencias entre la base de datos y el bus de mensajes.

Cuando ocurre una operación importante (ej: compra de activo):

1. Se guarda en la base de datos
2. Se guarda el evento en la tabla Outbox
3. Un worker interno publica el evento a RabbitMQ
4. Otros servicios actualizan su estado

Esto evita el problema conocido como **Dual Write Problem**.

---

## 🚀 Cómo ejecutar el proyecto

Requisitos:

* Docker
* Docker Compose

```bash
docker-compose up --build
```

Esto iniciará:

* SQL Server
* RabbitMQ
* Los microservicios

---

## 📌 Estado del proyecto

Proyecto en desarrollo activo.
El objetivo es construir una simulación realista de una plataforma de inversiones distribuida, priorizando decisiones arquitectónicas por sobre UI.

---

## 📚 Documentación técnica

La documentación completa de arquitectura se encuentra en:

`/ARCHITECTURE.md`



Aggregator Service----> utilizado en MarketData para agregar precios de diferentes fuentes externas y exponer un endpoint unificado.
Un Aggregator Service es un servicio cuya única responsabilidad es:
combinar datos de varios microservicios para producir una respuesta compuesta.
¿Aggregator es lo mismo que API Gateway? No, un API Gateway es un punto de entrada unificado para toda la plataforma, mientras que un Aggregator Service se enfoca en combinar datos específicos de ciertos servicios para una funcionalidad concreta.
Especialmente, Aggregator si posee lógica de negocio (ej: cálculos, transformaciones) mientras que API Gateway solo enruta y delega.
En nuestro sistema quedaría algo así:

Client
  ↓
API Gateway
  ↓
Transactions Service
Holdings Service
MarketData Service
Portfolio Service

DIFERENCIAS CLAVE:
| API Gateway       | Aggregator           |
| ----------------- | -------------------- |
| Routing           | composición de datos |
| Infraestructura   | lógica de dominio    |
| No calcula        | sí calcula           |
| No conoce negocio | conoce el modelo     |


//Nuevo elemento PositionLot---> Dentro de Position, se introduce el concepto de "Lot" para manejar compras y ventas de activos de manera más granular. Un Lot representa una cantidad específica de un activo adquirida en una transacción particular.
Dentro de Consumer de Holdings, al procesar un evento de compra o venta, se sigue la siguiente lógica:
1. Validar secuencia (Position)
2. Buscar/crear Lot (por currency)
3. Aplicar lógica (Lot)
4. Si queda vacío → eliminar Lot

¿Qué es PortfolioService?
👉 Un servicio que te devuelve esto:

Portfolio
 ├── Positions
 │    ├── SPY
 │    │    ├── TotalValue
 │    │    ├── PnL
 │    │    ├── % del portfolio
 │    │    └── Breakdown por moneda (USD / ARS)
 │
 └── TotalPortfolioValue
 
 Será una vista global del estado actual de la cartera, combinando datos de Holdings y MarketData.

 ✔ Portfolio → usa Ticker para precios
✔ MarketData → NO conoce InstrumentId
✔ Batch → optimización (podés hacerlo simple ahora)


PortfolioService
     ↓
IMarketDataClient (HTTP)
     ↓
MarketData API (microservicio)
     ↓
Proveedor externo (o fake por ahora)



Diseño correcto (IMPORTANTE)

Controller
   ↓
PriceService  ← 🔥 ORQUESTADOR
   ↓
IPriceService (cache)
   ↓
IPriceProvider (Finnhub, Yahoo, etc)
   ↓
HTTP externo