# Bonos de desempeño — Prueba técnica Full Stack

Módulo de RR. HH. para registrar bonos de desempeño a empleados (monto fijo o porcentaje del sueldo) y calcular bruto, retención y neto.

- Base de datos: SQL Server (BonusTest)
- Backend:.NET 10, ASP.NET Core Web API, Entity Framework Core
- Frontend: Angular 22, componentes standalone

## Cómo levantarlo

### 1. Base de datos
Ejecutar en orden, sobre SQL Server local:

database/01_schema_seed.sql   (base entregada, sin modificar)
database/02_bonus.sql         (tabla Bonus, claves foráneas, restricciones e índices)

La cadena de conexión está en `backend/src/BonusTest.Api/appsettings.json`.

### 2. Backend

cd backend/src/BonusTest.Api

Compilar el proyecto

Escucha en http://localhost:5106

### 3. Frontend
cd frontend
npm install
npx.cmd ng serve / ng serve / npx ng serve

http://localhost:4200

## Arquitectura del backend

Cuatro proyectos; cada uno solo conoce a los de más adentro:

| Capa | Contenido |
|---|---|
| **Domain** | Entidades y reglas puras: `Bonus`, `BonusCalculator`, `BusinessException` |
| **Application** | Casos de uso (`BonusService`), DTOs e interfaz `IBonusRepository` |
| **Infrastructure** | EF Core: `AppDbContext`, `BonusRepository`, configuración de tablas |
| **Api** | `BonusController` (solo HTTP) y manejo de errores |

Endpoints:

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/Bonus` | Listado paginado con filtros (área, empleado, periodo, estado) |
| GET | `/api/v1/Bonus/employee/{departmentCode}/{employeeNumber}` | Busca un empleado activo |
| POST | `/api/v1/Bonus` | Registra un bono |
| PUT | `/api/v1/Bonus/{bonusId}/cancel` | Anula (0201 → 0203) |
| DELETE | `/api/v1/Bonus/{bonusId}` | Elimina de forma lógica |

El usuario que opera se envía en el header `UserCode`. Los errores de negocio responden 409 con `{ code, description }`.

## Decisiones técnicas

### Base de datos
- **Clave foránea compuesta hacia `Employee`.** `Employee` tiene clave primaria `(DepartmentCode, EmployeeNumber)`; el número solo no es único entre áreas, así que `Bonus` guarda las dos columnas.
- **Periodo como `DATE` (primer día del mes).** Permite comparar y ordenar sin convertir textos. Un `CHECK` garantiza que sea día 1. La API recibe y devuelve `YYYY-MM`.
- **Un solo campo `BonusValue`** para el monto o el porcentaje, según `BonusTypeId`. Los `CHECK` validan que sea mayor a 0 y, si es porcentaje, hasta 100.
- **Bruto, retención, neto y tasa se guardan en la tabla.** Si luego cambia el sueldo o la retención del empleado, los bonos ya registrados no cambian.
- **Índice único filtrado** `(DepartmentCode, EmployeeNumber, Period) WHERE IsDeleted = 0 AND BonusStateCode <> '0203'`. Garantiza en la base de datos la regla de un bono activo por periodo, incluso con solicitudes simultáneas.
- **Índices de listado** filtrados a `IsDeleted = 0`: uno por área/empleado/periodo y otro por estado/periodo.
- **Eliminación lógica** con `IsDeleted`; auditoría con `CreatedBy/At` y `ModifiedBy/At`.
- Una clave foránea no puede limitar el estado a los de bono (`MasterStateCode = 02`), por eso un `CHECK` restringe `BonusStateCode` a `0201`, `0202` y `0203`.

### Backend
- **Las reglas viven en el dominio, no en el controlador.** `Bonus.Register` valida el monto y calcula; `Cancel` y `Delete` comprueban que el estado sea Registrado.
- **`BonusService` orquesta**: busca, valida lo que depende de la base de datos o del reloj (empleado activo, periodo, motivo activo, duplicado) y delega las reglas al dominio.
- **Redondeo** a 2 decimales con el medio hacia arriba (`MidpointRounding.AwayFromZero`).
- **Duplicados:** el servicio valida antes de guardar y el índice único cubre la concurrencia; `BonusRepository` convierte el error de SQL (2601/2627) en el mismo 409 `BONUS_ALREADY_EXISTS`.
- **Paginación en la base de datos** con `Skip/Take` (`OFFSET/FETCH`), con orden determinista por periodo y `BonusId`.
- **Filtro global `!IsDeleted`** en la configuración de EF para que ninguna consulta devuelva bonos eliminados.
- **`TimeProvider` inyectado** en lugar de `DateTime.Now`, para poder probar las reglas de fecha.

### Frontend
- **Los errores 409 los muestra solo el interceptor existente**; los componentes no repiten ese manejo.
- **Filtros aplicados al pulsar Buscar**, no mientras se escribe; la paginación conserva los filtros aplicados.
- **Vista previa del cálculo** en el modal con la misma fórmula que el backend (`core/utils/bonus-calculator.ts`). Es solo informativa: el backend recalcula siempre.
- **Validaciones del modal** (campos obligatorios, periodo no futuro, monto/porcentaje según tipo y sueldo) reutilizan una única función, `validateBonusValue`.
- **Confirmación** de anular y eliminar con un diálogo reutilizable (`shared/confirm-dialog`).
- Si eliminar deja vacía la última página, el listado retrocede a la última que exista.

## Pendiente

- **Punto extra 1:** pagar bono y distribuirlo por proyecto (`POST /api/v1/Bonus/{bonusId}/pay`, tabla de detalle y botón en la grilla). El estado `Pagado (0202)` ya está contemplado en el modelo, pero ningún flujo lo usa todavía.
- **Punto extra 2:** pruebas unitarias del cálculo (`BonusCalculator` es estático y sin dependencias, listo para probar).
- **Pruebas del frontend** y pruebas de integración de la API.
- **Revisión manual del frontend en el navegador:** compila y el backend está probado, pero el flujo visual completo no se ha recorrido.
- **Autenticación:** el usuario viene de un header (`UserCode`) tal como define el enunciado; no hay validación de identidad.
