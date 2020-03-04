using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WhatsBroken.Worker.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pipelines",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<Guid>(nullable: false),
                    AzDoId = table.Column<int>(nullable: false),
                    Path = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Project = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pipelines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestCases",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Project = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: false),
                    Method = table.Column<string>(nullable: false),
                    Arguments = table.Column<string>(nullable: true),
                    ArgumentHash = table.Column<string>(nullable: true),
                    Kind = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Builds",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<Guid>(nullable: false),
                    AzDoId = table.Column<int>(nullable: false),
                    PipelineId = table.Column<int>(nullable: false),
                    BuildNumber = table.Column<string>(nullable: false),
                    FinishedDate = table.Column<DateTime>(nullable: true),
                    SyncStartDate = table.Column<DateTime>(nullable: true),
                    SyncEndDate = table.Column<DateTime>(nullable: true),
                    ModelVersion = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Builds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Builds_Pipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "Pipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRuns",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<Guid>(nullable: false),
                    AzDoId = table.Column<int>(nullable: false),
                    BuildId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestRuns_Builds_BuildId",
                        column: x => x.BuildId,
                        principalTable: "Builds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(nullable: false),
                    CaseId = table.Column<int>(nullable: false),
                    Outcome = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResults_TestCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "TestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestResults_TestRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "TestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestResultDetails",
                columns: table => new
                {
                    TestResultId = table.Column<int>(nullable: false),
                    WebUrl = table.Column<string>(nullable: true),
                    SkipReason = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true),
                    StackTrace = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResultDetails", x => x.TestResultId);
                    table.ForeignKey(
                        name: "FK_TestResultDetails_TestResults_TestResultId",
                        column: x => x.TestResultId,
                        principalTable: "TestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Builds_PipelineId",
                table: "Builds",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_Builds_ProjectId_AzDoId",
                table: "Builds",
                columns: new[] { "ProjectId", "AzDoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_ProjectId_AzDoId",
                table: "Pipelines",
                columns: new[] { "ProjectId", "AzDoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_CaseId",
                table: "TestResults",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_RunId",
                table: "TestResults",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRuns_BuildId",
                table: "TestRuns",
                column: "BuildId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRuns_ProjectId_AzDoId",
                table: "TestRuns",
                columns: new[] { "ProjectId", "AzDoId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestResultDetails");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "TestCases");

            migrationBuilder.DropTable(
                name: "TestRuns");

            migrationBuilder.DropTable(
                name: "Builds");

            migrationBuilder.DropTable(
                name: "Pipelines");
        }
    }
}
