using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ledger_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_value = table.Column<decimal>(type: "numeric(20,2)", precision: 20, scale: 2, nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    entry_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ledger_transaction_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_ledger_entries_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ledger_entries_ledger_transactions_ledger_transaction_id",
                        column: x => x.ledger_transaction_id,
                        principalTable: "ledger_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_value = table.Column<decimal>(type: "numeric(20,2)", precision: 20, scale: 2, nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ledger_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.id);
                    table.CheckConstraint("ck_transfers_amount_positive", "amount_value > 0");
                    table.CheckConstraint("ck_transfers_different_accounts", "source_account_id <> destination_account_id");
                    table.ForeignKey(
                        name: "FK_transfers_accounts_destination_account_id",
                        column: x => x.destination_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfers_accounts_source_account_id",
                        column: x => x.source_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfers_ledger_transactions_ledger_transaction_id",
                        column: x => x.ledger_transaction_id,
                        principalTable: "ledger_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfer_reversals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ledger_transaction_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_reversals", x => x.id);
                    table.CheckConstraint("ck_transfer_reversals_completed_after_requested", "completed_at IS NULL OR completed_at >= requested_at");
                    table.ForeignKey(
                        name: "FK_transfer_reversals_ledger_transactions_ledger_transaction_id",
                        column: x => x.ledger_transaction_id,
                        principalTable: "ledger_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfer_reversals_transfers_transfer_id",
                        column: x => x.transfer_id,
                        principalTable: "transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entries_account_id",
                table: "ledger_entries",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entries_ledger_transaction_id",
                table: "ledger_entries",
                column: "ledger_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_reversals_ledger_transaction_id",
                table: "transfer_reversals",
                column: "ledger_transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transfer_reversals_status_requested_at",
                table: "transfer_reversals",
                columns: new[] { "status", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "IX_transfer_reversals_transfer_id",
                table: "transfer_reversals",
                column: "transfer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transfers_destination_account_id",
                table: "transfers",
                column: "destination_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_ledger_transaction_id",
                table: "transfers",
                column: "ledger_transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transfers_source_account_id",
                table: "transfers",
                column: "source_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ledger_entries");

            migrationBuilder.DropTable(
                name: "transfer_reversals");

            migrationBuilder.DropTable(
                name: "transfers");

            migrationBuilder.DropTable(
                name: "ledger_transactions");
        }
    }
}
