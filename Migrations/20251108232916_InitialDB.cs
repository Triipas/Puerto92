using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Puerto92.Migrations
{
    /// <inheritdoc />
    public partial class InitialDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Accion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UsuarioAccion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: true),
                    UsuarioAfectado = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DireccionIP = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DatosAdicionales = table.Column<string>(type: "TEXT", nullable: true),
                    Modulo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NivelSeveridad = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "getdate()"),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                    table.CheckConstraint("CK_Categorias_Orden_Rango", "[Orden] BETWEEN 1 AND 999");
                });

            migrationBuilder.CreateTable(
                name: "Locales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Distrito = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Ciudad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RUC = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PersonaContacto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "getdate()"),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utensilios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Unidad = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "getdate()"),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utensilios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CategoriaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Unidad = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PrecioCompra = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "getdate()"),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Productos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LocalId = table.Column<int>(type: "INTEGER", nullable: false),
                    EsPrimerIngreso = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordReseteada = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Locales_LocalId",
                        column: x => x.LocalId,
                        principalTable: "Locales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesKardex",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TipoKardex = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmpleadoId = table.Column<string>(type: "TEXT", nullable: false),
                    LocalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "getdate()"),
                    CreadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EsReasignacion = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AsignacionOriginalId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmpleadoOriginal = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MotivoReasignacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaReasignacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReasignadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RegistroIniciado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DatosParciales = table.Column<string>(type: "TEXT", nullable: true),
                    FechaNotificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NotificacionEnviada = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesKardex", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesKardex_AspNetUsers_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesKardex_Locales_LocalId",
                        column: x => x.LocalId,
                        principalTable: "Locales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UrlAccion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TextoAccion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DatosAdicionales = table.Column<string>(type: "TEXT", nullable: true),
                    Icono = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Prioridad = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Leida = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaLectura = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "getdate()"),
                    FechaExpiracion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MostrarPopup = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KardexBebidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AsignacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LocalId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpleadoId = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaFinalizacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KardexBebidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KardexBebidas_AsignacionesKardex_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "AsignacionesKardex",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KardexBebidas_AspNetUsers_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KardexBebidas_Locales_LocalId",
                        column: x => x.LocalId,
                        principalTable: "Locales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KardexBebidasDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KardexBebidasId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    InventarioInicial = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ingresos = table.Column<decimal>(type: "TEXT", nullable: false),
                    ConteoAlmacen = table.Column<decimal>(type: "TEXT", nullable: true),
                    ConteoRefri1 = table.Column<decimal>(type: "TEXT", nullable: true),
                    ConteoRefri2 = table.Column<decimal>(type: "TEXT", nullable: true),
                    ConteoRefri3 = table.Column<decimal>(type: "TEXT", nullable: true),
                    ConteoFinal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ventas = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiferenciaPorcentual = table.Column<decimal>(type: "TEXT", nullable: true),
                    TieneDiferenciaSignificativa = table.Column<bool>(type: "INTEGER", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", nullable: true),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KardexBebidasDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KardexBebidasDetalles_KardexBebidas_KardexBebidasId",
                        column: x => x.KardexBebidasId,
                        principalTable: "KardexBebidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KardexBebidasDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesKardex_EmpleadoId_Fecha",
                table: "AsignacionesKardex",
                columns: new[] { "EmpleadoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesKardex_Estado",
                table: "AsignacionesKardex",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesKardex_FechaCreacion",
                table: "AsignacionesKardex",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesKardex_LocalId_Fecha_TipoKardex",
                table: "AsignacionesKardex",
                columns: new[] { "LocalId", "Fecha", "TipoKardex" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LocalId",
                table: "AspNetUsers",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Accion",
                table: "AuditLogs",
                column: "Accion");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_FechaHora",
                table: "AuditLogs",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Modulo",
                table: "AuditLogs",
                column: "Modulo");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UsuarioAccion",
                table: "AuditLogs",
                column: "UsuarioAccion");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Tipo_Activo_Orden",
                table: "Categorias",
                columns: new[] { "Tipo", "Activo", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Tipo_Nombre",
                table: "Categorias",
                columns: new[] { "Tipo", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Tipo_Orden",
                table: "Categorias",
                columns: new[] { "Tipo", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KardexBebidas_AsignacionId",
                table: "KardexBebidas",
                column: "AsignacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KardexBebidas_EmpleadoId",
                table: "KardexBebidas",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_KardexBebidas_LocalId_Fecha_Estado",
                table: "KardexBebidas",
                columns: new[] { "LocalId", "Fecha", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_KardexBebidasDetalles_KardexBebidasId_Orden",
                table: "KardexBebidasDetalles",
                columns: new[] { "KardexBebidasId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_KardexBebidasDetalles_ProductoId",
                table: "KardexBebidasDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Locales_Codigo",
                table: "Locales",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_FechaExpiracion",
                table: "Notificaciones",
                column: "FechaExpiracion");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Tipo",
                table: "Notificaciones",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId_Leida_FechaCreacion",
                table: "Notificaciones",
                columns: new[] { "UsuarioId", "Leida", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CategoriaId_Activo",
                table: "Productos",
                columns: new[] { "CategoriaId", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Codigo",
                table: "Productos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Nombre",
                table: "Productos",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Categoria_Activo",
                table: "Proveedores",
                columns: new[] { "Categoria", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_RUC",
                table: "Proveedores",
                column: "RUC",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utensilios_Codigo",
                table: "Utensilios",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utensilios_Nombre",
                table: "Utensilios",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Utensilios_Tipo_Activo",
                table: "Utensilios",
                columns: new[] { "Tipo", "Activo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "KardexBebidasDetalles");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Utensilios");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "KardexBebidas");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "AsignacionesKardex");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Locales");
        }
    }
}
