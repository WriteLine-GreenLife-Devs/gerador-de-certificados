using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeradorCertificados.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarModuloCertificados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitacoesCertificado",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CursoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DataSolicitacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesCertificado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacoesCertificado_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Certificados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeAluno = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "text", nullable: true),
                    DataGeracao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificados_SolicitacoesCertificado_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "SolicitacoesCertificado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificados_SolicitacaoId",
                table: "Certificados",
                column: "SolicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesCertificado_CursoId",
                table: "SolicitacoesCertificado",
                column: "CursoId",
                unique: true,
                filter: "\"Status\" IN ('Pendente', 'GerandoCertificados', 'GerandoZip')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Certificados");

            migrationBuilder.DropTable(
                name: "SolicitacoesCertificado");
        }
    }
}
