# BonusTest · Bonos de desempeño
# Reto Tecnico Arnold Ramirez Zavaleta

Solución Full Stack para registrar y administrar bonos de desempeño. Incluye la funcionalidad obligatoria del reto y los dos puntos adicionales: pago distribuido por proyecto y pruebas unitarias.

## Stack

- SQL Server 2019+
- .NET 10, ASP.NET Core Web API y Entity Framework Core
- Angular 22 con componentes standalone y formularios reactivos
- xUnit para pruebas unitarias

## Puesta en marcha

### 1. Base de datos

Ejecutar los scripts respetando el orden:

1. `database/01_schema_seed.sql`
2. `database/02_bonus_module.sql`

Se puede utilizar una instancia local o iniciar SQL Server con Docker:

```bash
docker compose up -d
```

El contenedor usa `sa / BonusTest_2026!` solo para desarrollo. En un entorno compartido se debe definir la variable `MSSQL_SA_PASSWORD`.

### 2. Backend

La conexión predeterminada usa autenticación integrada de Windows:

```text
Server=localhost;Database=BonusTest;Trusted_Connection=True;TrustServerCertificate=True;
```

Para el contenedor se puede sobrescribir sin modificar archivos:

```powershell
$env:ConnectionStrings__BonusTest="Server=localhost,1433;Database=BonusTest;User Id=sa;Password=BonusTest_2026!;TrustServerCertificate=True"
dotnet run --project backend/src/BonusTest.Api
```

Swagger queda disponible en `http://localhost:5106/swagger` al ejecutar en Development.

### 3. Frontend

```bash
cd frontend
npm ci
npm start
```

Abrir `http://localhost:4200/bonuses`.

### 4. Pruebas

```bash
dotnet test backend/BonusTest.slnx
```

Los tests cubren monto fijo, porcentaje, retención cero, límites, redondeo `AwayFromZero` y la distribución exacta entre proyectos.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/Bonus` | Listado paginado con filtros |
| `GET` | `/api/v1/Bonus/employee/{departmentCode}/{employeeNumber}` | Busca y valida un empleado |
| `POST` | `/api/v1/Bonus` | Registra un bono |
| `PUT` | `/api/v1/Bonus/{bonusId}/cancel` | Anula un bono registrado |
| `DELETE` | `/api/v1/Bonus/{bonusId}` | Realiza eliminación lógica |
| `POST` | `/api/v1/Bonus/{bonusId}/pay` | Paga y distribuye el bruto por proyecto |

Ejemplo de registro:

```http
POST /api/v1/Bonus
UserCode: aramirez
Content-Type: application/json
```

```json
{
  "departmentCode": "TECH1",
  "employeeNumber": 101,
  "period": "2026-10",
  "bonusTypeId": 2,
  "value": 10,
  "reasonCode": "000001"
}
```

## Decisiones técnicas

### Persistencia

- El periodo se guarda como `DATE` usando siempre el primer día del mes. Esto mantiene semántica temporal, permite ordenar y filtrar sin conversiones de texto.
- Se guardan sueldo y tasa de retención como *snapshot*, además de bruto, retención y neto. Un cambio futuro en el empleado no altera el histórico pagado.
- Un índice único filtrado protege la regla de un bono activo por empleado y periodo, incluso ante dos solicitudes concurrentes.
- `RowVersion` aplica concurrencia optimista en anulación, eliminación y pago.
- La eliminación es lógica y conserva usuario y fecha de modificación.

### Backend

- Los controladores se limitan al contrato HTTP. Las reglas y la coordinación viven en Application; los cálculos puros y transiciones válidas, en Domain; EF Core y transacciones, en Infrastructure.
- Todas las consultas de lectura usan `AsNoTracking`. La paginación ejecuta `Count`, `Skip` y `Take` en SQL Server.
- El cálculo monetario usa `decimal` y `MidpointRounding.AwayFromZero` en cada monto.
- El pago crea el detalle y cambia a `0202` dentro de una transacción. El último proyecto recibe la diferencia de redondeo para que la suma sea exactamente igual al bruto.
- Los conflictos de negocio mantienen el contrato entregado: HTTP 409 con `{ code, description }`.

### Frontend

- Formularios reactivos y validación dinámica: el porcentaje admite hasta 100 y el monto fijo no supera el sueldo del empleado validado.
- La vista previa reproduce el cálculo del backend; el servidor continúa siendo la fuente de verdad.
- Paginación real desde el servidor, estados de carga, confirmaciones propias y acciones disponibles únicamente para bonos registrados.
- El interceptor concentra `UserCode` y los mensajes de error, evitando duplicar manejo HTTP en el componente.
- `OnPush`, signals y `track` en las colecciones reducen trabajo de detección de cambios.

## Estructura

```text
database/
  01_schema_seed.sql
  02_bonus_module.sql
backend/
  src/BonusTest.Domain
  src/BonusTest.Application
  src/BonusTest.Infrastructure
  src/BonusTest.Api
  tests/BonusTest.UnitTests
frontend/
  src/app/core
  src/app/features/bonuses
```

## Opccion de mejora

El reto define `UserCode` como identidad de auditoría y por eso el frontend lo envía directamente. se obtendría de los *claims* de un token validado. También se moverían credenciales a un gestor de secretos, se agregarían pruebas de integración con SQL Server y observabilidad centralizada.
