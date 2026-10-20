using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConstructionFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceTenantActorMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_events_users_actor_user_id",
                table: "audit_events");

            migrationBuilder.DropForeignKey(
                name: "FK_daily_reports_users_author_user_id",
                table: "daily_reports");

            migrationBuilder.DropForeignKey(
                name: "FK_daily_reports_users_reviewed_by",
                table: "daily_reports");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_users_reviewed_by",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_reviewed_by",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_daily_reports_author_user_id",
                table: "daily_reports");

            migrationBuilder.DropIndex(
                name: "IX_daily_reports_reviewed_by",
                table: "daily_reports");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_actor_user_id",
                table: "audit_events");

            migrationBuilder.CreateIndex(
                name: "IX_documents_tenant_id_reviewed_by",
                table: "documents",
                columns: new[] { "tenant_id", "reviewed_by" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_reports_tenant_id_author_user_id",
                table: "daily_reports",
                columns: new[] { "tenant_id", "author_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_reports_tenant_id_reviewed_by",
                table: "daily_reports",
                columns: new[] { "tenant_id", "reviewed_by" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_tenant_id_actor_user_id",
                table: "audit_events",
                columns: new[] { "tenant_id", "actor_user_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_audit_events_tenant_memberships_tenant_id_actor_user_id",
                table: "audit_events",
                columns: new[] { "tenant_id", "actor_user_id" },
                principalTable: "tenant_memberships",
                principalColumns: new[] { "tenant_id", "user_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_daily_reports_tenant_memberships_tenant_id_author_user_id",
                table: "daily_reports",
                columns: new[] { "tenant_id", "author_user_id" },
                principalTable: "tenant_memberships",
                principalColumns: new[] { "tenant_id", "user_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_daily_reports_tenant_memberships_tenant_id_reviewed_by",
                table: "daily_reports",
                columns: new[] { "tenant_id", "reviewed_by" },
                principalTable: "tenant_memberships",
                principalColumns: new[] { "tenant_id", "user_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_tenant_memberships_tenant_id_reviewed_by",
                table: "documents",
                columns: new[] { "tenant_id", "reviewed_by" },
                principalTable: "tenant_memberships",
                principalColumns: new[] { "tenant_id", "user_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_events_tenant_memberships_tenant_id_actor_user_id",
                table: "audit_events");

            migrationBuilder.DropForeignKey(
                name: "FK_daily_reports_tenant_memberships_tenant_id_author_user_id",
                table: "daily_reports");

            migrationBuilder.DropForeignKey(
                name: "FK_daily_reports_tenant_memberships_tenant_id_reviewed_by",
                table: "daily_reports");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_tenant_memberships_tenant_id_reviewed_by",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_tenant_id_reviewed_by",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_daily_reports_tenant_id_author_user_id",
                table: "daily_reports");

            migrationBuilder.DropIndex(
                name: "IX_daily_reports_tenant_id_reviewed_by",
                table: "daily_reports");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_tenant_id_actor_user_id",
                table: "audit_events");

            migrationBuilder.CreateIndex(
                name: "IX_documents_reviewed_by",
                table: "documents",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_daily_reports_author_user_id",
                table: "daily_reports",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_reports_reviewed_by",
                table: "daily_reports",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_actor_user_id",
                table: "audit_events",
                column: "actor_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_events_users_actor_user_id",
                table: "audit_events",
                column: "actor_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_daily_reports_users_author_user_id",
                table: "daily_reports",
                column: "author_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_daily_reports_users_reviewed_by",
                table: "daily_reports",
                column: "reviewed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_users_reviewed_by",
                table: "documents",
                column: "reviewed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
