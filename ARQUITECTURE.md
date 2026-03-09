# Arquitectura — InvestmentTracker v1

## Objetivo

El propósito del sistema es modelar una plataforma de inversiones distribuida donde cada responsabilidad del negocio se encuentra aislada en un microservicio independiente.

El foco principal del proyecto es demostrar:

* Desacoplamiento entre servicios
* Consistencia eventual
* Mensajería asíncrona
* Manejo de fallas reales en sistemas distribuidos

---

## Microservicios

| Servicio     | Responsabilidad                        |
| ------------ | -------------------------------------- |
| Users        | Identidad y perfiles                   |
| Transactions | Registro histórico de operaciones      |
| Holdings     | Estado actual de posiciones            |
| MarketData   | Integración con proveedores de precios |

Separación conceptual:

* **Identidad** → Users
* **Historial** → Transactions
* **Estado actual** → Holdings
* **Datos externos** → MarketData

---

## Principio fundamental

Los microservicios:

❌ No comparten base de datos
❌ No se referencian a nivel de código
✔ Solo se comunican mediante contratos (eventos o HTTP)

---

## Comunicación entre servicios

### Asíncrona (principal)

RabbitMQ + MassTransit

Ejemplo:

`TransactionCreatedEvent`

Flujo:

Transactions → publica evento → Holdings → actualiza posiciones

### Síncrona (secundaria)

Holdings consulta precios actuales a MarketData mediante HTTP.

---

## El problema: Dual Write

Un servicio necesita:

1. Persistir datos
2. Publicar un evento

Si uno falla:

DB OK + MQ FAIL → el sistema queda inconsistente.

Esto es inevitable en sistemas distribuidos.

---

## Solución: Transactional Outbox

Cada operación guarda el evento en la misma transacción de base de datos.

Tabla técnica:

| Id | Type | Content | OccurredOnUtc | ProcessedOnUtc |

Luego un proceso en segundo plano publica los eventos pendientes.

Esto garantiza:

> Si la DB confirmó la operación, el evento eventualmente será publicado.

---

## Outbox Publisher

Implementado como `BackgroundService`.

Responsabilidades:

1. Leer OutboxMessages no procesados
2. Publicarlos en RabbitMQ
3. Marcar `ProcessedOnUtc`

Importante:

No modifica el evento de negocio.
Solo actualiza el estado técnico del mensaje.

---

## Consistencia Eventual

El sistema no es inmediatamente consistente.

Ejemplo:

Compra realizada → Holdings actualizado segundos después.

Este comportamiento es esperado y es propio de sistemas distribuidos reales.

---

## Manejo de orden de mensajes

Cada evento contiene:

* Timestamp

Si un consumidor recibe un evento más antiguo que el último procesado, lo ignora.

Previene sobrescrituras incorrectas.

---

## Clean Architecture

Cada servicio sigue:

Controller → Application Service → Domain → Repository → DbContext

Reglas:

* El dominio no depende de infraestructura
* Application define contratos
* Infrastructure implementa detalles técnicos

---

## Manejo de errores

La lógica de negocio NO maneja excepciones técnicas:

No pertenece al dominio:

* SqlException
* TimeoutException
* DbUpdateException

Pertenecen a la infraestructura o middleware HTTP.

---

## Beneficios de la arquitectura

* Escalable
* Reemplazable por partes
* Resistente a fallos
* Extensible (ej: agregar Notifications o Analytics sin modificar servicios existentes)

Ejemplo:

Un `UserCreatedEvent` puede ser consumido por:

* Portfolio
* Analytics
* Email Notifications

Sin modificar Users.
