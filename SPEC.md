# SPEC — Sistema de Administración de Sociedad de Fomento de Sierra de los Padres

## 1. Objetivo

Desarrollar una aplicación web para administrar una Sociedad de Fomento de Sierra de los Padres, Mar del Plata.

El sistema debe permitir:

* Administrar socios.
* Gestionar cuotas sociales mensuales.
* Registrar e imputar pagos.
* Consultar el estado de cuenta de cada socio.
* Permitir que cada socio consulte su propia deuda.
* Llevar una contabilidad básica de ingresos y egresos.
* Obtener información resumida de la situación económica y de los socios.

El objetivo inicial es desarrollar un **MVP simple, económico y fácil de mantener**.

No se deben implementar funcionalidades innecesarias que no estén contempladas en esta especificación.

---

# 2. Usuarios

Existen dos tipos de usuarios.

## 2.1 Administrador

Tiene acceso completo al sistema.

Puede:

* Gestionar socios.
* Gestionar valores de cuotas.
* Consultar cuotas.
* Registrar pagos.
* Anular pagos.
* Consultar estados de cuenta.
* Registrar otros ingresos.
* Registrar egresos.
* Consultar movimientos contables.
* Consultar el dashboard.

## 2.2 Socio

Tiene acceso únicamente a su propia información.

El socio se identifica mediante su DNI.

Puede consultar:

* Sus datos.
* Sus cuotas.
* Sus cuotas pagadas.
* Sus cuotas pendientes.
* Sus cuotas vencidas.
* Su importe total adeudado.

No puede acceder a información de otros socios ni a la administración.

---

# 3. Socios

Cada socio debe almacenar:

* DNI
* Nombre
* Apellido
* Dirección
* Fecha de nacimiento
* Teléfono
* Email (opcional)
* Fecha de alta
* Estado

Estados posibles:

* Activo
* Inactivo

El DNI identifica de forma única al socio.

## 3.1 Alta

Al registrar un socio, la fecha de alta se obtiene de la fecha actual del sistema en la zona horaria de negocio `America/Argentina/Buenos_Aires`.

El DNI se normaliza eliminando puntos, guiones y espacios antes de validar su unicidad y almacenarlo.

El socio comienza a tener obligación de pago desde el mes correspondiente a su fecha de alta.

No existe comportamiento especial si el alta ocurre a mitad de mes.

Ejemplo:

Alta: 20/09/2026

La cuota de septiembre se genera normalmente por el importe completo.

El alta genera únicamente la obligación del mes actual y no genera cuotas futuras.

Si no existe un valor de cuota vigente para el mes de alta, la operación completa se rechaza: no se crea el socio, su período de actividad ni una obligación incompleta.

## 3.2 Baja

Al pasar un socio a estado `Inactivo`:

* La baja utiliza la fecha actual del sistema en `America/Argentina/Buenos_Aires`.
* En el MVP no se permiten bajas retroactivas.
* No se eliminan sus datos.
* No se elimina su historial.
* No se eliminan, anulan ni modifican sus cuotas ya generadas.
* La cuota del mes de baja continúa siendo exigible por el importe completo.
* No se eliminan sus pagos.
* No se eliminan sus movimientos contables relacionados.
* Deja de generar nuevas cuotas para períodos posteriores al mes de baja.

## 3.3 Reactivación

Un socio inactivo puede volver a estar activo.

La reactivación se considera una nueva alta a efectos de generación de cuotas.

La cuota del mes de reactivación se genera por el importe completo, independientemente del día de reactivación.

La reactivación debe ocurrir en una fecha posterior a la última baja; no se permite reactivar el mismo día para evitar períodos superpuestos.

Si no existe un valor de cuota vigente para el mes de reactivación, la operación completa se rechaza y no se modifica el socio, sus períodos ni sus obligaciones.

Si ya existe una obligación para el socio y el período de reactivación, no debe generarse una cuota duplicada ni modificarse su período de origen, tarifa o importe.

Si no existe, se crea una única obligación asociada al nuevo período de actividad.

La reactivación no genera cuotas futuras.

Las cuotas correspondientes al período anterior permanecen en el historial y no se eliminan.

A partir del mes de reactivación comienzan nuevamente las obligaciones.

---

# 4. Cuotas

Cada socio activo tiene una cuota social mensual.

Todos los socios pagan el mismo importe correspondiente al valor de cuota vigente para cada período.

Cada cuota corresponde a un período mensual:

* Desde el día 1 del mes.
* Hasta el último día de ese mes.

## 4.1 Generación

Las cuotas deben generarse automáticamente.

No deben generarse cuotas futuras innecesariamente.

Cuando comienza un nuevo mes, el sistema debe generar las cuotas correspondientes a los socios activos.

La generación debe respetar la fecha de alta/reactivación del socio.

## 4.2 Estado de cuota

Una cuota puede tener:

* Pendiente
* Pagada
* Vencida

### Pendiente

La cuota está dentro de su período y todavía no fue pagada.

Ejemplo:

15/09/2026 → cuota septiembre = Pendiente.

### Pagada

La cuota fue completamente cancelada mediante un pago.

### Vencida

Si llega el día 1 del mes siguiente y la cuota continúa impaga, pasa a estar `Vencida`.

Ejemplo:

30/09/2026 → septiembre = Pendiente.

01/10/2026 → septiembre = Vencida.

Una cuota vencida continúa siendo pagable.

---

# 5. Valores de cuota

El valor de la cuota puede cambiar con el tiempo.

El sistema debe conservar un historial de valores.

Cada cambio debe indicar:

* Importe.
* Mes desde el cual entra en vigencia.

Ejemplo:

| Vigente desde | Importe |
| ------------- | ------: |
| 01/01/2026    |  $5.000 |
| 01/07/2026    |  $7.000 |
| 01/10/2026    |  $9.000 |

Por lo tanto:

* Enero-junio → $5.000.
* Julio-septiembre → $7.000.
* Octubre en adelante → $9.000.

## Regla fundamental

Una vez generada una cuota, su importe queda establecido y **no debe modificarse posteriormente**.

Modificar el valor de cuota no debe modificar cuotas históricas.

Una vez que un valor de cuota fue utilizado para generar al menos una obligación, no puede modificarse ni eliminarse.

Para aplicar otro importe debe crearse una nueva vigencia.

---

# 6. Pagos

Los administradores pueden registrar pagos realizados por los socios.

Cada pago debe almacenar:

* Socio.
* Fecha.
* Importe.
* Medio de pago.
* Cuotas imputadas.

Medios de pago:

* Efectivo.
* Transferencia.
* Débito automático.

Los pagos de débito automático son registrados manualmente por el administrador.

No se requiere integración bancaria en el MVP.

---

# 7. Imputación de pagos

El administrador selecciona manualmente las cuotas que el pago está cancelando.

Un pago puede cancelar una o varias cuotas.

Ejemplo:

```text
Pago: $21.000

☑ Julio 2026  $7.000
☑ Agosto 2026 $7.000
☑ Septiembre 2026 $7.000
```

Total: $21.000.

## 7.1 Pagos adelantados

Está permitido pagar cuotas futuras.

Ejemplo:

En septiembre, el socio puede pagar:

* Septiembre.
* Octubre.
* Noviembre.
* Diciembre.

El administrador selecciona manualmente esas cuotas.

Una cuota puede quedar `Pagada` aunque su período todavía no haya comenzado.

Si una obligación futura ya fue generada y posteriormente el socio es dado de baja, la obligación se conserva.

Si el pago adelantado que la cubría se anula, la obligación queda impaga y será exigible cuando llegue su período, aunque el socio continúe inactivo.

## 7.2 Pagos parciales

No están permitidos.

Una cuota debe pagarse completamente.

El sistema debe impedir registrar un pago cuyo importe no coincida con la suma de las cuotas seleccionadas.

Ejemplo:

Cuota: $7.000.

No se puede imputar:

$3.500.

## 7.3 Anulación de pagos

Un administrador puede anular un pago registrado incorrectamente.

El pago no debe eliminarse físicamente de la base de datos.

Debe quedar registrado como anulado para conservar el historial.

Al anular un pago:

* Las cuotas asociadas dejan de estar pagadas.
* Las cuotas vuelven a su estado correspondiente según la fecha actual.
* Si el período ya terminó, quedan `Vencidas`.
* Si el período está vigente, quedan `Pendientes`.
* Si el período todavía no comenzó, quedan disponibles para pago futuro.
* El ingreso contable generado por ese pago debe revertirse/anularse automáticamente.

---

# 8. Estado de cuenta

Los administradores pueden consultar el estado de cuenta de cualquier socio.

La búsqueda debe permitir utilizar el DNI.

Debe mostrar:

* Datos del socio.
* Estado del socio.
* Historial de cuotas.
* Importe de cada cuota.
* Estado de cada cuota.
* Historial de pagos.
* Medio de pago.
* Fecha de pago.
* Total pagado.
* Total adeudado.

## 8.1 Cálculo de deuda

La deuda se obtiene sumando los importes de las cuotas que:

* Ya comenzaron a ser exigibles.
* No están pagadas.

Las cuotas futuras no forman parte de la deuda.

Ejemplo:

Hoy: 23/09/2026.

| Período    | Importe | Estado    |
| ---------- | ------: | --------- |
| Julio      |  $7.000 | Pagada    |
| Agosto     |  $7.000 | Vencida   |
| Septiembre |  $7.000 | Pendiente |
| Octubre    |  $9.000 | Futura    |

Total adeudado:

**$14.000**

El 01/10/2026, si septiembre continúa impaga:

* Agosto → Vencida.
* Septiembre → Vencida.
* Octubre → Pendiente.

Total adeudado:

**$23.000**

---

# 9. Portal del socio

El socio puede ingresar utilizando únicamente su DNI.

No tendrá acceso a información administrativa.

Debe poder consultar:

* Nombre.
* Apellido.
* Estado.
* Cuotas.
* Pagos.
* Cuotas pendientes.
* Cuotas vencidas.
* Total adeudado.

El socio solamente puede consultar su propia información.

---

# 10. Contabilidad

El sistema tendrá una contabilidad básica.

Se divide en:

* Ingresos.
* Egresos.

## 10.1 Ingresos por cuotas

Cuando se registra un pago de cuotas:

1. Se registra el pago.
2. Se imputan las cuotas.
3. Se genera automáticamente un ingreso contable.

El administrador no debe registrar nuevamente ese ingreso de forma manual.

El ingreso debe estar relacionado con el pago que lo originó.

La fecha del ingreso contable debe ser exactamente igual a la fecha del pago que lo originó.

Si el pago se anula, el ingreso correspondiente debe anularse automáticamente, conservando su historial y excluyéndolo de los totales activos.

## 10.2 Otros ingresos

El administrador puede registrar ingresos que no provengan de cuotas.

Ejemplos:

* Donaciones.
* Alquiler de instalaciones.
* Otros ingresos.

Un ingreso manual debe contener como mínimo:

* Fecha.
* Concepto.
* Importe.

Los ingresos manuales pueden crearse y anularse, pero no editarse después de su creación.

Para corregir un ingreso manual se debe anular el movimiento original y crear uno nuevo.

La anulación conserva el historial y excluye el ingreso de los totales activos.

## 10.3 Egresos

Los egresos se registran manualmente.

Cada egreso contiene:

* Fecha.
* Concepto.
* Importe.

Los egresos pueden crearse y anularse, pero no editarse después de su creación.

Para corregir un egreso se debe anular el movimiento original y crear uno nuevo.

La anulación conserva el historial y excluye el egreso de los totales activos.

No se requieren categorías de gastos en el MVP.

No se requieren comprobantes adjuntos.

---

# 11. Resumen contable

El sistema debe permitir consultar:

* Total de ingresos.
* Total de egresos.
* Saldo.

Fórmula:

Saldo = Ingresos - Egresos

Debe poder filtrarse por período.

Como mínimo:

* Mes actual.
* Mes anterior.
* Año.
* Rango de fechas.

---

# 12. Dashboard administrativo

El administrador tendrá un dashboard inicial con información resumida.

Debe mostrar como mínimo:

### Socios

* Socios activos.
* Socios inactivos.

### Cuotas

* Cuotas pendientes.
* Cuotas vencidas.
* Importe adeudado.

### Economía

* Ingresos del período.
* Egresos del período.
* Saldo del período.

El dashboard debe priorizar información útil y no debe convertirse en un sistema de reportes complejo.

---

# 13. Búsquedas y filtros

Debe ser posible buscar socios por:

* DNI.
* Nombre.
* Apellido.

Los pagos deberían poder filtrarse por:

* Socio.
* Fecha.
* Medio de pago.

Los movimientos contables deberían poder filtrarse por:

* Fecha.
* Tipo: ingreso/egreso.
* Concepto.

---

# 14. Exportación

Como funcionalidad útil pero sencilla, el sistema debería permitir exportar listados a CSV/Excel cuando corresponda.

Inicialmente podrían exportarse:

* Socios.
* Pagos.
* Movimientos contables.

No se requiere un sistema avanzado de generación de reportes.

---

# 15. Autenticación

## Administradores

Los administradores deben autenticarse exclusivamente mediante email y contraseña.

Las contraseñas deben almacenarse utilizando un hash seguro, nunca en texto plano.

La sesión administrativa utiliza una cookie cifrada, `HttpOnly`, `Secure` y `SameSite=Strict`, con rol `Admin` validado por el backend.

El login, logout y las operaciones administrativas que modifican datos requieren protección antiforgery.

El primer administrador puede crearse de forma idempotente mediante `AdminBootstrap__Email` y `AdminBootstrap__Password`, después de aplicar manualmente las migraciones.

En este milestone no existe cambio, recuperación ni restablecimiento de contraseña.

## Socios

Los socios ingresan utilizando únicamente su DNI, según el requerimiento definido para el MVP.

El sistema debe impedir que un socio acceda a información perteneciente a otro socio.

---

# 16. Reglas generales importantes

1. El DNI identifica unívocamente al socio.
2. Todos los socios tienen el mismo valor de cuota para un período determinado.
3. El valor de cuota puede cambiar a partir de un mes determinado.
4. Cambiar el valor de cuota no modifica cuotas históricas.
5. Una tarifa utilizada no se modifica ni elimina; un cambio requiere una nueva vigencia.
6. Las cuotas comienzan a ser exigibles el día 1 del mes.
7. Una cuota impaga pasa a vencida el día 1 del mes siguiente.
8. No existen pagos parciales.
9. Un pago puede imputarse a varias cuotas.
10. Se pueden pagar cuotas futuras.
11. El administrador selecciona manualmente las cuotas que cancela un pago.
12. Las cuotas pagadas no se vuelven a cobrar.
13. Anular un pago libera nuevamente las cuotas que había cancelado.
14. Anular un pago también anula el ingreso contable correspondiente.
15. La fecha del ingreso automático coincide con la fecha del pago.
16. Los socios inactivos no generan nuevas cuotas.
17. La baja usa la fecha actual y no puede ser retroactiva en el MVP.
18. La baja no elimina, anula ni modifica obligaciones ya generadas.
19. La información histórica de socios inactivos se conserva.
20. Reactivar un socio genera la cuota completa desde el mes de reactivación sin duplicar una obligación existente.
21. Las cuotas futuras no forman parte de la deuda hasta que comienza su período.
22. Una obligación futura ya generada se conserva aunque el socio sea dado de baja.
23. Los pagos de cuotas generan automáticamente ingresos contables.
24. Los otros ingresos se pueden crear y anular, pero no editar.
25. Los egresos se pueden crear y anular, pero no editar.
26. Los movimientos contables anulados conservan su historial y se excluyen de los totales activos.
27. No se requieren comprobantes en el MVP.
28. No se requiere integración bancaria.
29. El sistema debe priorizar simplicidad y facilidad de mantenimiento.

---

# 17. Criterios de aceptación principales

El sistema se considerará funcionalmente correcto cuando pueda realizarse el siguiente flujo:

### Flujo completo

1. Crear una cuota de $5.000 vigente desde enero.
2. Crear un socio con alta en septiembre.
3. El sistema genera su cuota de septiembre por $5.000.
4. La cuota aparece como pendiente.
5. Registrar un nuevo valor de cuota de $7.000 desde octubre.
6. La cuota de septiembre continúa siendo de $5.000.
7. Al comenzar octubre se genera la cuota de octubre por $7.000.
8. Registrar un pago de $12.000.
9. Seleccionar septiembre y octubre.
10. Ambas cuotas pasan a pagadas.
11. Se genera automáticamente un ingreso contable de $12.000.
12. Consultar el estado de cuenta y verificar que la deuda sea $0.
13. Registrar una cuota de noviembre.
14. No pagarla.
15. Al comenzar diciembre debe aparecer como vencida.
16. La deuda debe incluir noviembre.
17. Registrar un pago que cancele noviembre.
18. La cuota pasa a pagada.
19. El ingreso contable se genera automáticamente.
20. Anular el pago.
21. La cuota vuelve a estar vencida.
22. El ingreso contable asociado queda anulado/revertido.
23. Dar de baja al socio.
24. El socio conserva todo su historial.
25. No se generan nuevas cuotas mientras esté inactivo.
26. Reactivar al socio.
27. A partir del mes de reactivación vuelve a generar cuotas.

---

# 18. Prioridad del desarrollo

El proyecto debe desarrollarse como MVP.

Orden recomendado:

1. Base del proyecto.
2. Autenticación de administrador.
3. Gestión de socios.
4. Gestión de valores de cuota.
5. Generación de cuotas.
6. Registro e imputación de pagos.
7. Estado de cuenta.
8. Contabilidad automática.
9. Egresos y otros ingresos.
10. Portal del socio.
11. Dashboard.
12. Filtros y exportaciones.
13. Pruebas y correcciones.
14. Deploy.

No implementar funcionalidades fuera de este alcance sin una necesidad concreta.

---

# 19. Principio de desarrollo

La aplicación debe priorizar:

* Simplicidad.
* Bajo costo.
* Mantenimiento sencillo.
* Código claro.
* Evitar sobreingeniería.
* Evitar microservicios innecesarios.
* Evitar dependencias pagas cuando exista una alternativa gratuita.
* Utilizar servicios gratuitos o de muy bajo costo siempre que sean adecuados.

La arquitectura concreta puede ser definida por la herramienta de desarrollo siempre que respete los requisitos funcionales y las reglas de negocio de este documento.
