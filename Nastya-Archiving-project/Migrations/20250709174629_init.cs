using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nastya_Archiving_project.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Arciv_DocDscrp",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    IsoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arciv_DocDscrp", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Arciv_Docs_Refrences",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeadReferenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LinkedRfrenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Dscription = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PakegID = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EditDate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Docs_Refrences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Arciv_SubDocDscrp",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arciv_SubDocDscrp", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Arciving_Docs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefrenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocID = table.Column<int>(type: "int", nullable: true),
                    DocNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DocSource = table.Column<int>(type: "int", nullable: true),
                    DocTarget = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    WordsTosearch = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImgURL = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DocTitle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BoxfileNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocType = table.Column<int>(type: "int", nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EditDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    theyear = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(year,[DocDate]))", stored: false),
                    TheWay = table.Column<int>(type: "int", nullable: true),
                    SystemID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    sequre = table.Column<int>(type: "int", nullable: true),
                    DocSize = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileType = table.Column<int>(type: "int", nullable: true),
                    TheMonth = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(month,[DocDate]))", stored: false),
                    SubDocType = table.Column<int>(type: "int", nullable: true),
                    fourth = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    HaseBakuped = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    ReferenceTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arciving_Docs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Arciving_Docs_Deleted",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    RefrenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocID = table.Column<int>(type: "int", nullable: true),
                    DocNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    DocSource = table.Column<int>(type: "int", nullable: true),
                    DocTarget = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    WordsTosearch = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImgURL = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DocTitle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BoxfileNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocType = table.Column<int>(type: "int", nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EditDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    theyear = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(year,[DocDate]))", stored: false),
                    TheWay = table.Column<int>(type: "int", nullable: true),
                    SystemID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    sequre = table.Column<int>(type: "int", nullable: true),
                    DocSize = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileType = table.Column<int>(type: "int", nullable: true),
                    TheMonth = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(month,[DocDate]))", stored: false),
                    SubDocType = table.Column<int>(type: "int", nullable: true),
                    fourth = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    HaseBakuped = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arciving_Docs_Deleted", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "GeneralOptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnyCript_ = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Language_ = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValueSql: "((0))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralOptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gp_AccountingUnits",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gp_AccountingUnits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gp_Branches",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(88)", maxLength: 88, nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Gp_ColumnsInfo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DscrpA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TblName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TheSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserInserted = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    ParameterTable = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, defaultValue: "-"),
                    ControlType = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    Required = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_ReportColumns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gp_Departments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(88)", maxLength: 88, nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gp_Departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gp_MonthDscrp",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Monthdscrp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gp_MonthDscrp", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gp_ParametersPages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    URL = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Modl = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Serials = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gp_ParametersPages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gp_SubDepartments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gp_SubDepartments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Gp_SystemImages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    img = table.Column<byte[]>(type: "image", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gp_SystemImages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Gp_SystemImagesP",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DscrpA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Gp_TablesInfo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DscrpA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TblType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Table_1", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gt_Customizations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TheTable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TheTableA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TheValue = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TheType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true),
                    DefaultValue = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gt_Customizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gt_CustomizationsP",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TheTable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TheTableA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TheType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TheSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gt_CustomizationsP", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_ArciningMainFolder",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StorePath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_ArciningMainFolder", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_ArcivingPoints",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    StartWith = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StorePath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BackupPath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_ArcivingPoints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_BlindMail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_BlindMail", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_DefaultBody",
                columns: table => new
                {
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "P_DocsWay",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_DocsWay", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_FileType",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Extention = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Code = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_FileType", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_JobTitle",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StepID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_JobTitle", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_Mail_Samples",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Dscrp = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_Samples", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_MailAdresses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_MailAdresses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_MailHosts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ServerDomen = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ServerPort = table.Column<int>(type: "int", nullable: true),
                    E_Mail = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Pass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EnabledSSL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_MailHosts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_Notes_Samples",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_Notes_Samples", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_Operation",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_Operation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_Organizations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_Organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "P_Packegs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_Packegs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "T_DocumentsRelations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentDocID = table.Column<int>(type: "int", nullable: true),
                    RelatedDocID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_DocumentsRelations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "T_FileTransferring",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefrenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FROM_ = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TO_ = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SendDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Readed = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_FileTransferring", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "T_MailArchiving",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BodyStr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SenderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SendDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SendTo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CCto = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FileURL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HostAdress = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_MailArchiving", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "T_MailTargets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    usertargetMail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_MailTargets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "T_Writed_Documents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DocDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoxfileNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocType = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EditDate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(getdate())"),
                    theyear = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(year,[DocDate]))", stored: false),
                    TheMonth = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(month,[DocDate]))", stored: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Writed_Documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TMP_CorroptedDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    TheState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMP_CorroptedDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Realname = table.Column<string>(type: "nvarchar(172)", maxLength: 172, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(172)", maxLength: 172, nullable: true),
                    UserPassword = table.Column<string>(type: "nvarchar(172)", maxLength: 172, nullable: true),
                    GroupID = table.Column<int>(type: "int", nullable: true),
                    permtype = table.Column<string>(type: "nvarchar(172)", maxLength: 172, nullable: true),
                    adminst = table.Column<string>(type: "nvarchar(172)", maxLength: 172, nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(172)", maxLength: 172, nullable: true),
                    EditDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    GobStep = table.Column<int>(type: "int", nullable: true),
                    DepariID = table.Column<int>(type: "int", nullable: true),
                    DevisionID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    AsWFUser = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    ASMailCenter = table.Column<int>(type: "int", nullable: true),
                    JobTitle = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users_ArchivingPointsPermissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    ArchivingpointID = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_ArchivingPointsPermissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users_Doctype_Permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    DoctypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_Doctype_Permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users_Editing",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Model = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TblName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TblNameA = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RecordID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RecordData = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EditDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IPAdress = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_Editing", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users_FileBrowing",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    FileType = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_FileBrowing", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users_OptionPermissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    AddParameters = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    AllowDelete = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    AllowAddToOther = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    AllowViewTheOther = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    AllowSendMail = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    AllowDownload = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_OptionPermissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users_ReportColumns",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrimeryKey = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    ColumnName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ColumnNamedisplay = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TblName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TblNameA = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TheSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserInserted = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Seq = table.Column<int>(type: "int", nullable: true),
                    joiningPart = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, defaultValue: "-"),
                    ConStr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ControlType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    Required = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    ParameterTable = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true, defaultValue: "-"),
                    ASParameter = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    SecondaryTable = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true, defaultValue: "-"),
                    SecondaryKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true, defaultValue: "-")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_ReportColumns_1", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usersgroups",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    groupdscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(172)", maxLength: 172, nullable: true),
                    EditDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usersgroups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usersinterfaces",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pagedscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pageurl = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    outputtype = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    program = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    Serials = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usersinterfaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "UsersPerm",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    PermType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PermID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "userspermissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    groupid = table.Column<int>(type: "int", nullable: true),
                    pageid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userspermissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WF_P_Precedence",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaxDate = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_P_Precedence", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WF_P_ProcessType",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_P_ProcessType", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WF_ProcessSequence",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseID = table.Column<int>(type: "int", nullable: true),
                    CaseSequence = table.Column<int>(type: "int", nullable: true),
                    FromUserID = table.Column<int>(type: "int", nullable: true),
                    FromUserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ToUserID = table.Column<int>(type: "int", nullable: true),
                    ToUserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SendDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OpeninigDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Signature = table.Column<byte[]>(type: "image", nullable: true),
                    Approved = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    CC = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Redirected = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    UnderHand = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_ProcessSequence", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WF_T_ArchivingCreatedDocs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseID = table.Column<int>(type: "int", nullable: true),
                    DocID = table.Column<int>(type: "int", nullable: true),
                    DocNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceID = table.Column<int>(type: "int", nullable: true),
                    TargetID = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EditDate = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(getdate())"),
                    theyear = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(year,[DocDate]))", stored: false),
                    themonth = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(month,[DocDate]))", stored: false),
                    ProcessType = table.Column<int>(type: "int", nullable: true),
                    ImgURL = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    DocIDType = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true, defaultValue: "_د")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archiving_CreatedDocs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WF_T_Attached",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseID = table.Column<int>(type: "int", nullable: true),
                    CaseSequence = table.Column<int>(type: "int", nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    ImgURL = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FileNam = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RefrenceNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_P_Documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WF_T_ExportDocNo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExportDocNo = table.Column<string>(type: "nchar(20)", fixedLength: true, maxLength: 20, nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EditDate = table.Column<DateOnly>(type: "date", nullable: true),
                    theyear = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(year,[EditDate]))", stored: false),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    ProcessTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_ExportDocNo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WF_T_Processing",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseID = table.Column<int>(type: "int", nullable: true),
                    PrecedenceID = table.Column<int>(type: "int", nullable: true),
                    RefrenceNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CaseCreatedBY = table.Column<int>(type: "int", nullable: true),
                    CaseTarget = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CaseClosed = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    DocType = table.Column<int>(type: "int", nullable: true),
                    DocID = table.Column<int>(type: "int", nullable: true),
                    DocNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DocDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProcessType = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    theyear = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(year,[DocDate]))", stored: false),
                    themonth = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datepart(month,[DocDate]))", stored: false),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_T_Processing", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WF_T_Samples",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dscrp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Structures = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DepartID = table.Column<int>(type: "int", nullable: true),
                    AccountUnitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WF_T_Samples", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Arciv_DocDscrp");

            migrationBuilder.DropTable(
                name: "Arciv_Docs_Refrences");

            migrationBuilder.DropTable(
                name: "Arciv_SubDocDscrp");

            migrationBuilder.DropTable(
                name: "Arciving_Docs");

            migrationBuilder.DropTable(
                name: "Arciving_Docs_Deleted");

            migrationBuilder.DropTable(
                name: "GeneralOptions");

            migrationBuilder.DropTable(
                name: "gp_AccountingUnits");

            migrationBuilder.DropTable(
                name: "gp_Branches");

            migrationBuilder.DropTable(
                name: "Gp_ColumnsInfo");

            migrationBuilder.DropTable(
                name: "gp_Departments");

            migrationBuilder.DropTable(
                name: "gp_MonthDscrp");

            migrationBuilder.DropTable(
                name: "gp_ParametersPages");

            migrationBuilder.DropTable(
                name: "gp_SubDepartments");

            migrationBuilder.DropTable(
                name: "Gp_SystemImages");

            migrationBuilder.DropTable(
                name: "Gp_SystemImagesP");

            migrationBuilder.DropTable(
                name: "Gp_TablesInfo");

            migrationBuilder.DropTable(
                name: "gt_Customizations");

            migrationBuilder.DropTable(
                name: "gt_CustomizationsP");

            migrationBuilder.DropTable(
                name: "P_ArciningMainFolder");

            migrationBuilder.DropTable(
                name: "P_ArcivingPoints");

            migrationBuilder.DropTable(
                name: "P_BlindMail");

            migrationBuilder.DropTable(
                name: "P_DefaultBody");

            migrationBuilder.DropTable(
                name: "P_DocsWay");

            migrationBuilder.DropTable(
                name: "P_FileType");

            migrationBuilder.DropTable(
                name: "P_JobTitle");

            migrationBuilder.DropTable(
                name: "P_Mail_Samples");

            migrationBuilder.DropTable(
                name: "P_MailAdresses");

            migrationBuilder.DropTable(
                name: "P_MailHosts");

            migrationBuilder.DropTable(
                name: "P_Notes_Samples");

            migrationBuilder.DropTable(
                name: "P_Operation");

            migrationBuilder.DropTable(
                name: "P_Organizations");

            migrationBuilder.DropTable(
                name: "P_Packegs");

            migrationBuilder.DropTable(
                name: "T_DocumentsRelations");

            migrationBuilder.DropTable(
                name: "T_FileTransferring");

            migrationBuilder.DropTable(
                name: "T_MailArchiving");

            migrationBuilder.DropTable(
                name: "T_MailTargets");

            migrationBuilder.DropTable(
                name: "T_Writed_Documents");

            migrationBuilder.DropTable(
                name: "TMP_CorroptedDocuments");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "Users_ArchivingPointsPermissions");

            migrationBuilder.DropTable(
                name: "users_Doctype_Permissions");

            migrationBuilder.DropTable(
                name: "Users_Editing");

            migrationBuilder.DropTable(
                name: "Users_FileBrowing");

            migrationBuilder.DropTable(
                name: "Users_OptionPermissions");

            migrationBuilder.DropTable(
                name: "users_ReportColumns");

            migrationBuilder.DropTable(
                name: "usersgroups");

            migrationBuilder.DropTable(
                name: "usersinterfaces");

            migrationBuilder.DropTable(
                name: "UsersPerm");

            migrationBuilder.DropTable(
                name: "userspermissions");

            migrationBuilder.DropTable(
                name: "WF_P_Precedence");

            migrationBuilder.DropTable(
                name: "WF_P_ProcessType");

            migrationBuilder.DropTable(
                name: "WF_ProcessSequence");

            migrationBuilder.DropTable(
                name: "WF_T_ArchivingCreatedDocs");

            migrationBuilder.DropTable(
                name: "WF_T_Attached");

            migrationBuilder.DropTable(
                name: "WF_T_ExportDocNo");

            migrationBuilder.DropTable(
                name: "WF_T_Processing");

            migrationBuilder.DropTable(
                name: "WF_T_Samples");
        }
    }
}
