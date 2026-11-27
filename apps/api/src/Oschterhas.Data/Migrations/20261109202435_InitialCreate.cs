using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oschterhas.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    identity_subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_platform_admin = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    event_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    custom_domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: true),
                    custom_domain_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    custom_domain_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    custom_domain_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    custom_domain_last_check_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    custom_domain_failed_checks = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    lat = table.Column<double>(type: "double precision", nullable: false),
                    lon = table.Column<double>(type: "double precision", nullable: false),
                    elevation = table.Column<double>(type: "double precision", nullable: true),
                    source_waypoint_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    found_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    found_by = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hares", x => x.id);
                    table.ForeignKey(
                        name: "fk_hares_instances_instance_id",
                        column: x => x.instance_id,
                        principalTable: "instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instance_organizers",
                columns: table => new
                {
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instance_organizers", x => new { x.instance_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_instance_organizers_app_users_user_id",
                        column: x => x.user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_instance_organizers_instances_instance_id",
                        column: x => x.instance_id,
                        principalTable: "instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "found_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hare_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nickname = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_found_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_found_events_app_users_user_id",
                        column: x => x.user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_found_events_hares_hare_id",
                        column: x => x.hare_id,
                        principalTable: "hares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_users_email",
                table: "app_users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_app_users_identity_issuer_identity_subject",
                table: "app_users",
                columns: new[] { "identity_issuer", "identity_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_found_events_hare_id",
                table: "found_events",
                column: "hare_id");

            migrationBuilder.CreateIndex(
                name: "ix_found_events_user_id",
                table: "found_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_hares_instance_id_number",
                table: "hares",
                columns: new[] { "instance_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hares_instance_id_source_waypoint_id",
                table: "hares",
                columns: new[] { "instance_id", "source_waypoint_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instance_organizers_user_id",
                table: "instance_organizers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_instances_custom_domain",
                table: "instances",
                column: "custom_domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instances_slug",
                table: "instances",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "found_events");

            migrationBuilder.DropTable(
                name: "instance_organizers");

            migrationBuilder.DropTable(
                name: "hares");

            migrationBuilder.DropTable(
                name: "app_users");

            migrationBuilder.DropTable(
                name: "instances");
        }
    }
}
