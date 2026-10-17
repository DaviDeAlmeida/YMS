using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace YMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OwnerIdOnEveryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_batch_item_errors_batch_items_batch_item_id",
                table: "batch_item_errors");

            migrationBuilder.DropForeignKey(
                name: "fk_batch_items_batches_batch_id",
                table: "batch_items");

            migrationBuilder.DropForeignKey(
                name: "fk_product_eans_products_product_id",
                table: "product_eans");

            migrationBuilder.DropIndex(
                name: "ix_product_eans_product_id",
                table: "product_eans");

            migrationBuilder.DropIndex(
                name: "ix_batches_owner_id",
                table: "batches");

            migrationBuilder.DropIndex(
                name: "ix_batch_item_errors_batch_item_id",
                table: "batch_item_errors");

            // PostgreSQL has no cast from integer to uuid: existing rows get a fresh uuid v7 (PostgreSQL 18).
            migrationBuilder.Sql("ALTER TABLE product_eans ALTER COLUMN id DROP IDENTITY;");
            migrationBuilder.Sql("ALTER TABLE product_eans ALTER COLUMN id TYPE uuid USING uuidv7();");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "product_eans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "batch_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("ALTER TABLE batch_item_errors ALTER COLUMN id DROP IDENTITY;");
            migrationBuilder.Sql("ALTER TABLE batch_item_errors ALTER COLUMN id TYPE uuid USING uuidv7();");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "batch_item_errors",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Existing rows take the Owner of their parent, so the composite foreign keys below hold.
            migrationBuilder.Sql("UPDATE product_eans e SET owner_id = p.owner_id FROM products p WHERE p.id = e.product_id;");
            migrationBuilder.Sql("UPDATE batch_items i SET owner_id = b.owner_id FROM batches b WHERE b.id = i.batch_id;");
            migrationBuilder.Sql("UPDATE batch_item_errors e SET owner_id = i.owner_id FROM batch_items i WHERE i.id = e.batch_item_id;");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_products_owner_id_id",
                table: "products",
                columns: new[] { "owner_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_batches_owner_id_id",
                table: "batches",
                columns: new[] { "owner_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_batch_items_owner_id_id",
                table: "batch_items",
                columns: new[] { "owner_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_eans_owner_id_product_id",
                table: "product_eans",
                columns: new[] { "owner_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_batch_items_owner_id_batch_id",
                table: "batch_items",
                columns: new[] { "owner_id", "batch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_batch_item_errors_owner_id_batch_item_id",
                table: "batch_item_errors",
                columns: new[] { "owner_id", "batch_item_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_batch_item_errors_batch_items_owner_id_batch_item_id",
                table: "batch_item_errors",
                columns: new[] { "owner_id", "batch_item_id" },
                principalTable: "batch_items",
                principalColumns: new[] { "owner_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_batch_items_batches_owner_id_batch_id",
                table: "batch_items",
                columns: new[] { "owner_id", "batch_id" },
                principalTable: "batches",
                principalColumns: new[] { "owner_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_product_eans_products_owner_id_product_id",
                table: "product_eans",
                columns: new[] { "owner_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "owner_id", "id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_batch_item_errors_batch_items_owner_id_batch_item_id",
                table: "batch_item_errors");

            migrationBuilder.DropForeignKey(
                name: "fk_batch_items_batches_owner_id_batch_id",
                table: "batch_items");

            migrationBuilder.DropForeignKey(
                name: "fk_product_eans_products_owner_id_product_id",
                table: "product_eans");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_products_owner_id_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_product_eans_owner_id_product_id",
                table: "product_eans");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_batches_owner_id_id",
                table: "batches");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_batch_items_owner_id_id",
                table: "batch_items");

            migrationBuilder.DropIndex(
                name: "ix_batch_items_owner_id_batch_id",
                table: "batch_items");

            migrationBuilder.DropIndex(
                name: "ix_batch_item_errors_owner_id_batch_item_id",
                table: "batch_item_errors");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "product_eans");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "batch_items");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "batch_item_errors");

            // No cast from uuid back to integer either: the key column is recreated and renumbered.
            foreach (var table in new[] { "product_eans", "batch_item_errors" })
            {
                migrationBuilder.DropPrimaryKey(name: $"pk_{table}", table: table);
                migrationBuilder.DropColumn(name: "id", table: table);
                migrationBuilder.AddColumn<int>(
                    name: "id",
                    table: table,
                    type: "integer",
                    nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                migrationBuilder.AddPrimaryKey(name: $"pk_{table}", table: table, column: "id");
            }

            migrationBuilder.CreateIndex(
                name: "ix_product_eans_product_id",
                table: "product_eans",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_batches_owner_id",
                table: "batches",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_batch_item_errors_batch_item_id",
                table: "batch_item_errors",
                column: "batch_item_id");

            migrationBuilder.AddForeignKey(
                name: "fk_batch_item_errors_batch_items_batch_item_id",
                table: "batch_item_errors",
                column: "batch_item_id",
                principalTable: "batch_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_batch_items_batches_batch_id",
                table: "batch_items",
                column: "batch_id",
                principalTable: "batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_product_eans_products_product_id",
                table: "product_eans",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
