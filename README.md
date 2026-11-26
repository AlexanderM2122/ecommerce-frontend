# Ecommerce frontend

Aplicación ASP.NET Core MVC para consultar el catálogo, administrar el carrito y simular compras. Requiere el SDK de .NET 10 y el backend Node.js con MySQL.

## Ejecutar en local

1. Clona el [backend](https://github.com/AlexanderM2122/ecommerce-backend) y, en su carpeta, copia `.env.example` a `.env`. Configura los datos de MySQL, `JWT_SECRET` y `SERVER_PORT=3000`. Crea la base de datos indicada en `DB_DATABASE`.
2. En la carpeta del backend, instala las dependencias, ejecuta las migraciones e inicia la API.

   ```sh
   npm ci
   npx --yes sequelize-cli@6 db:migrate
   npm start
   ```

   Si la base está vacía, puedes cargar los datos de demostración con `npx --yes sequelize-cli@6 db:seed:all` antes de iniciar la API.

3. En la carpeta del frontend, ejecuta lo siguiente.

   ```sh
   dotnet dev-certs https --trust
   dotnet run --launch-profile https
   ```

4. Abre [https://localhost:7275](https://localhost:7275) e inicia sesión. Desde **Comprar**, agrega un producto y abre **Carrito** para actualizar cantidades, quitar productos o confirmar la compra simulada. La confirmación crea el pedido, vacía el carrito y abre el detalle. **Mis pedidos** muestra las compras de la cuenta y **Pedidos** permite al administrador consultar todas.
