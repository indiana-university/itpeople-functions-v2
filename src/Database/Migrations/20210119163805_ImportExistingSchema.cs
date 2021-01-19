using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Database.Migrations
{
    public partial class ImportExistingSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

/*
            migrationBuilder.CreateTable(
                name: "building_relationships",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_building_relationships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buildings",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buildings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(nullable: false),
                    description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "historical_people",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historical_people", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hr_people",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    net_id = table.Column<string>(nullable: true),
                    name = table.Column<string>(nullable: true),
                    name_first = table.Column<string>(nullable: true),
                    name_last = table.Column<string>(nullable: true),
                    position = table.Column<string>(nullable: true),
                    campus = table.Column<string>(nullable: true),
                    campus_phone = table.Column<string>(nullable: true),
                    campus_email = table.Column<string>(nullable: true),
                    hr_department = table.Column<string>(nullable: true),
                    hr_department_description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hr_people", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unit_member_tools",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_member_tools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "support_relationships",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_relationships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tool_permissions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tool_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tools",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(nullable: true),
                    ad_path = table.Column<string>(nullable: true),
                    department_scoped = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unit_members",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    permissions = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "units",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "people",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    net_id = table.Column<string>(nullable: false),
                    name = table.Column<string>(nullable: false),
                    name_first = table.Column<string>(nullable: false),
                    name_last = table.Column<string>(nullable: false),
                    position = table.Column<string>(nullable: false),
                    location = table.Column<string>(nullable: false),
                    campus = table.Column<string>(nullable: false),
                    campus_phone = table.Column<string>(nullable: false),
                    campus_email = table.Column<string>(nullable: false),
                    expertise = table.Column<string>(nullable: true),
                    notes = table.Column<string>(nullable: true),
                    photo_url = table.Column<string>(nullable: true),
                    responsibilities = table.Column<int>(nullable: false),
                    is_service_admin = table.Column<bool>(nullable: false),
                    department_id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_people", x => x.id);
                    table.ForeignKey(
                        name: "fk_people_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "public",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_people_department_id",
                schema: "public",
                table: "people",
                column: "department_id");
*/

            migrationBuilder.Sql(@"
--
-- PostgreSQL database dump
--

-- Dumped from database version 11.7.14
-- Dumped by pg_dump version 12.2

-- Started on 2021-01-15 10:13:55 EST

-- SET statement_timeout = 0;
-- SET lock_timeout = 0;
-- SET idle_in_transaction_session_timeout = 0;
-- SET client_encoding = 'UTF8';
-- SET standard_conforming_strings = on;
-- SELECT pg_catalog.set_config('search_path', '', false);
-- SET check_function_bodies = false;
-- SET xmloption = content;
-- SET client_min_messages = warning;
-- SET row_security = off;

--
-- TOC entry 19 (class 2615 OID 2200)
-- Name: public; Type: SCHEMA; Schema: -; Owner: edb
--

-- DROP SCHEMA public CASCADE;
-- CREATE SCHEMA public;

-- -- ALTER SCHEMA public OWNER TO edb;

--
-- TOC entry 4836 (class 0 OID 0)
-- Dependencies: 19
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: edb
--

-- COMMENT ON SCHEMA public IS 'standard public schema';



SET default_tablespace = '';

--
-- TOC entry 415 (class 1259 OID 42914)
-- Name: audit; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.audit (
    id integer NOT NULL,
    table_modified text NOT NULL,
    column_modified text NOT NULL,
    ""timestamp"" timestamp without time zone,
    username text NOT NULL,
    change_type text NOT NULL,
    value text NOT NULL
);


-- ALTER TABLE public.audit OWNER TO itpeople_owner;

--
-- TOC entry 414 (class 1259 OID 42912)
-- Name: audit_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.audit_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.audit_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4838 (class 0 OID 0)
-- Dependencies: 414
-- Name: audit_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.audit_id_seq OWNED BY public.audit.id;


--
-- TOC entry 422 (class 1259 OID 53671)
-- Name: building_relationships; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.building_relationships (
    id integer NOT NULL,
    unit_id integer NOT NULL,
    building_id integer NOT NULL
);


-- ALTER TABLE public.building_relationships OWNER TO itpeople_owner;

--
-- TOC entry 421 (class 1259 OID 53669)
-- Name: building_relationships_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.building_relationships_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.building_relationships_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4840 (class 0 OID 0)
-- Dependencies: 421
-- Name: building_relationships_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.building_relationships_id_seq OWNED BY public.building_relationships.id;


--
-- TOC entry 420 (class 1259 OID 53658)
-- Name: buildings; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.buildings (
    id integer NOT NULL,
    name text NOT NULL,
    code text NOT NULL,
    address text,
    city text NOT NULL,
    state text,
    country text,
    post_code text
);


-- ALTER TABLE public.buildings OWNER TO itpeople_owner;

--
-- TOC entry 419 (class 1259 OID 53656)
-- Name: buildings_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.buildings_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.buildings_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4842 (class 0 OID 0)
-- Dependencies: 419
-- Name: buildings_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.buildings_id_seq OWNED BY public.buildings.id;


--
-- TOC entry 402 (class 1259 OID 38522)
-- Name: departments; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.departments (
    id integer NOT NULL,
    name text NOT NULL,
    description text NOT NULL,
    display_units boolean DEFAULT false NOT NULL
);


-- ALTER TABLE public.departments OWNER TO itpeople_owner;

--
-- TOC entry 401 (class 1259 OID 38520)
-- Name: departments_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.departments_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.departments_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4844 (class 0 OID 0)
-- Dependencies: 401
-- Name: departments_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.departments_id_seq OWNED BY public.departments.id;


--
-- TOC entry 416 (class 1259 OID 44725)
-- Name: historical_people; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.historical_people (
    netid text NOT NULL,
    metadata json NOT NULL,
    removed_on timestamp without time zone
);


-- ALTER TABLE public.historical_people OWNER TO itpeople_owner;

--
-- TOC entry 418 (class 1259 OID 45224)
-- Name: hr_people; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.hr_people (
    id integer NOT NULL,
    netid text NOT NULL,
    name text NOT NULL,
    ""position"" text,
    campus text NOT NULL,
    campus_phone text,
    campus_email text,
    hr_department text NOT NULL,
    hr_department_desc text,
    name_first text,
    name_last text
);


-- ALTER TABLE public.hr_people OWNER TO itpeople_owner;

--
-- TOC entry 417 (class 1259 OID 45222)
-- Name: hr_people_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.hr_people_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.hr_people_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4847 (class 0 OID 0)
-- Dependencies: 417
-- Name: hr_people_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.hr_people_id_seq OWNED BY public.hr_people.id;


--
-- TOC entry 409 (class 1259 OID 38597)
-- Name: logs; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.logs (
    ""timestamp"" timestamp without time zone,
    level character varying(50),
    elapsed integer,
    status integer,
    method text,
    function text,
    parameters text,
    query text,
    detail text,
    exception text,
    ip_address text,
    netid text,
    content text
);


-- ALTER TABLE public.logs OWNER TO itpeople_owner;

--
-- TOC entry 423 (class 1259 OID 239852)
-- Name: logs_automation; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.logs_automation (
    ""timestamp"" timestamp without time zone,
    level character varying(50),
    invocation_id uuid NOT NULL,
    function_name text NOT NULL,
    message text,
    properties json
);


-- ALTER TABLE public.logs_automation OWNER TO itpeople_owner;

--
-- TOC entry 404 (class 1259 OID 38536)
-- Name: people; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.people (
    id integer NOT NULL,
    netid text NOT NULL,
    name text NOT NULL,
    ""position"" text NOT NULL,
    location text NOT NULL,
    campus text NOT NULL,
    campus_phone text NOT NULL,
    campus_email text NOT NULL,
    expertise text,
    notes text NOT NULL,
    photo_url text NOT NULL,
    responsibilities integer DEFAULT 0 NOT NULL,
    tools integer DEFAULT 7 NOT NULL,
    department_id integer,
    is_service_admin boolean DEFAULT false NOT NULL,
    name_first text,
    name_last text
);


-- ALTER TABLE public.people OWNER TO itpeople_owner;

--
-- TOC entry 403 (class 1259 OID 38534)
-- Name: people_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.people_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.people_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4851 (class 0 OID 0)
-- Dependencies: 403
-- Name: people_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.people_id_seq OWNED BY public.people.id;


--
-- TOC entry 406 (class 1259 OID 38556)
-- Name: support_relationships; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.support_relationships (
    id integer NOT NULL,
    unit_id integer NOT NULL,
    department_id integer NOT NULL
);


-- ALTER TABLE public.support_relationships OWNER TO itpeople_owner;

--
-- TOC entry 405 (class 1259 OID 38554)
-- Name: support_relationships_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.support_relationships_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.support_relationships_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4853 (class 0 OID 0)
-- Dependencies: 405
-- Name: support_relationships_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.support_relationships_id_seq OWNED BY public.support_relationships.id;


--
-- TOC entry 411 (class 1259 OID 42553)
-- Name: tools; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.tools (
    id integer NOT NULL,
    name text NOT NULL,
    description text NOT NULL,
    department_scoped boolean DEFAULT false NOT NULL,
    ad_path text DEFAULT ''::text NOT NULL
);


-- ALTER TABLE public.tools OWNER TO itpeople_owner;

--
-- TOC entry 410 (class 1259 OID 42551)
-- Name: tools_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.tools_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.tools_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4855 (class 0 OID 0)
-- Dependencies: 410
-- Name: tools_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.tools_id_seq OWNED BY public.tools.id;


--
-- TOC entry 413 (class 1259 OID 42564)
-- Name: unit_member_tools; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.unit_member_tools (
    id integer NOT NULL,
    membership_id integer,
    tool_id integer
);


-- ALTER TABLE public.unit_member_tools OWNER TO itpeople_owner;

--
-- TOC entry 412 (class 1259 OID 42562)
-- Name: unit_member_tools_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.unit_member_tools_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.unit_member_tools_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4857 (class 0 OID 0)
-- Dependencies: 412
-- Name: unit_member_tools_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.unit_member_tools_id_seq OWNED BY public.unit_member_tools.id;


--
-- TOC entry 408 (class 1259 OID 38574)
-- Name: unit_members; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.unit_members (
    id integer NOT NULL,
    unit_id integer NOT NULL,
    person_id integer,
    title text,
    role integer DEFAULT 2 NOT NULL,
    percentage integer DEFAULT 100 NOT NULL,
    tools integer DEFAULT 0 NOT NULL,
    permissions integer DEFAULT 2 NOT NULL,
    notes text DEFAULT ''::text NOT NULL
);


-- ALTER TABLE public.unit_members OWNER TO itpeople_owner;

--
-- TOC entry 407 (class 1259 OID 38572)
-- Name: unit_members_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.unit_members_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.unit_members_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4859 (class 0 OID 0)
-- Dependencies: 407
-- Name: unit_members_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.unit_members_id_seq OWNED BY public.unit_members.id;


--
-- TOC entry 400 (class 1259 OID 38506)
-- Name: units; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.units (
    id integer NOT NULL,
    name text NOT NULL,
    description text NOT NULL,
    url text,
    parent_id integer,
    email text
);


-- ALTER TABLE public.units OWNER TO itpeople_owner;

--
-- TOC entry 399 (class 1259 OID 38504)
-- Name: units_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.units_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.units_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4861 (class 0 OID 0)
-- Dependencies: 399
-- Name: units_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.units_id_seq OWNED BY public.units.id;


--
-- TOC entry 398 (class 1259 OID 38495)
-- Name: versioninfo; Type: TABLE; Schema: public; Owner: itpeople_owner
--

CREATE TABLE public.versioninfo (
    id integer NOT NULL,
    version bigint NOT NULL,
    appliedon timestamp with time zone,
    description text NOT NULL
);


-- ALTER TABLE public.versioninfo OWNER TO itpeople_owner;

--
-- TOC entry 397 (class 1259 OID 38493)
-- Name: versioninfo_id_seq; Type: SEQUENCE; Schema: public; Owner: itpeople_owner
--

CREATE SEQUENCE public.versioninfo_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ALTER TABLE public.versioninfo_id_seq OWNER TO itpeople_owner;

--
-- TOC entry 4863 (class 0 OID 0)
-- Dependencies: 397
-- Name: versioninfo_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: itpeople_owner
--

ALTER SEQUENCE public.versioninfo_id_seq OWNED BY public.versioninfo.id;


--
-- TOC entry 4554 (class 2604 OID 16727)
-- Name: audit id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.audit ALTER COLUMN id SET DEFAULT nextval('public.audit_id_seq'::regclass);


--
-- TOC entry 4557 (class 2604 OID 16728)
-- Name: building_relationships id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.building_relationships ALTER COLUMN id SET DEFAULT nextval('public.building_relationships_id_seq'::regclass);


--
-- TOC entry 4556 (class 2604 OID 16729)
-- Name: buildings id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.buildings ALTER COLUMN id SET DEFAULT nextval('public.buildings_id_seq'::regclass);


--
-- TOC entry 4538 (class 2604 OID 16730)
-- Name: departments id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.departments ALTER COLUMN id SET DEFAULT nextval('public.departments_id_seq'::regclass);


--
-- TOC entry 4555 (class 2604 OID 16731)
-- Name: hr_people id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.hr_people ALTER COLUMN id SET DEFAULT nextval('public.hr_people_id_seq'::regclass);


--
-- TOC entry 4542 (class 2604 OID 16732)
-- Name: people id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.people ALTER COLUMN id SET DEFAULT nextval('public.people_id_seq'::regclass);


--
-- TOC entry 4543 (class 2604 OID 16733)
-- Name: support_relationships id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.support_relationships ALTER COLUMN id SET DEFAULT nextval('public.support_relationships_id_seq'::regclass);


--
-- TOC entry 4552 (class 2604 OID 16734)
-- Name: tools id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.tools ALTER COLUMN id SET DEFAULT nextval('public.tools_id_seq'::regclass);


--
-- TOC entry 4553 (class 2604 OID 16735)
-- Name: unit_member_tools id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_member_tools ALTER COLUMN id SET DEFAULT nextval('public.unit_member_tools_id_seq'::regclass);


--
-- TOC entry 4549 (class 2604 OID 16736)
-- Name: unit_members id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_members ALTER COLUMN id SET DEFAULT nextval('public.unit_members_id_seq'::regclass);


--
-- TOC entry 4536 (class 2604 OID 16737)
-- Name: units id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.units ALTER COLUMN id SET DEFAULT nextval('public.units_id_seq'::regclass);


--
-- TOC entry 4535 (class 2604 OID 16738)
-- Name: versioninfo id; Type: DEFAULT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.versioninfo ALTER COLUMN id SET DEFAULT nextval('public.versioninfo_id_seq'::regclass);


--
-- TOC entry 4588 (class 2606 OID 16739)
-- Name: audit audit_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.audit
    ADD CONSTRAINT audit_pkey PRIMARY KEY (id);


--
-- TOC entry 4598 (class 2606 OID 16740)
-- Name: building_relationships building_relationships_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.building_relationships
    ADD CONSTRAINT building_relationships_pkey PRIMARY KEY (id);


--
-- TOC entry 4600 (class 2606 OID 16741)
-- Name: building_relationships building_relationships_unique_unitid_buildingid; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.building_relationships
    ADD CONSTRAINT building_relationships_unique_unitid_buildingid UNIQUE (unit_id, building_id);


--
-- TOC entry 4594 (class 2606 OID 16742)
-- Name: buildings buildings_code_key; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.buildings
    ADD CONSTRAINT buildings_code_key UNIQUE (code);


--
-- TOC entry 4596 (class 2606 OID 16743)
-- Name: buildings buildings_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.buildings
    ADD CONSTRAINT buildings_pkey PRIMARY KEY (id);


--
-- TOC entry 4563 (class 2606 OID 16744)
-- Name: departments departments_name_key; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.departments
    ADD CONSTRAINT departments_name_key UNIQUE (name);


--
-- TOC entry 4565 (class 2606 OID 16745)
-- Name: departments departments_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.departments
    ADD CONSTRAINT departments_pkey PRIMARY KEY (id);


--
-- TOC entry 4590 (class 2606 OID 16746)
-- Name: hr_people hr_people_netid_key; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.hr_people
    ADD CONSTRAINT hr_people_netid_key UNIQUE (netid);


--
-- TOC entry 4592 (class 2606 OID 16747)
-- Name: hr_people hr_people_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.hr_people
    ADD CONSTRAINT hr_people_pkey PRIMARY KEY (id);


--
-- TOC entry 4567 (class 2606 OID 16748)
-- Name: people people_netid_key; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.people
    ADD CONSTRAINT people_netid_key UNIQUE (netid);


--
-- TOC entry 4569 (class 2606 OID 16749)
-- Name: people people_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.people
    ADD CONSTRAINT people_pkey PRIMARY KEY (id);


--
-- TOC entry 4571 (class 2606 OID 16750)
-- Name: support_relationships support_relationships_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.support_relationships
    ADD CONSTRAINT support_relationships_pkey PRIMARY KEY (id);


--
-- TOC entry 4573 (class 2606 OID 16751)
-- Name: support_relationships support_relationships_unique_unitid_departmentid; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.support_relationships
    ADD CONSTRAINT support_relationships_unique_unitid_departmentid UNIQUE (unit_id, department_id);


--
-- TOC entry 4580 (class 2606 OID 16752)
-- Name: tools tools_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.tools
    ADD CONSTRAINT tools_pkey PRIMARY KEY (id);


--
-- TOC entry 4582 (class 2606 OID 16753)
-- Name: tools unique_name; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.tools
    ADD CONSTRAINT unique_name UNIQUE (name);


--
-- TOC entry 4584 (class 2606 OID 16754)
-- Name: unit_member_tools unit_member_tools_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_member_tools
    ADD CONSTRAINT unit_member_tools_pkey PRIMARY KEY (id);


--
-- TOC entry 4586 (class 2606 OID 16755)
-- Name: unit_member_tools unit_member_tools_unique_toolid_membershipid; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_member_tools
    ADD CONSTRAINT unit_member_tools_unique_toolid_membershipid UNIQUE (tool_id, membership_id);


--
-- TOC entry 4576 (class 2606 OID 16756)
-- Name: unit_members unit_members_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_members
    ADD CONSTRAINT unit_members_pkey PRIMARY KEY (id);


--
-- TOC entry 4578 (class 2606 OID 16757)
-- Name: unit_members unit_members_unique_unitid_personid; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_members
    ADD CONSTRAINT unit_members_unique_unitid_personid UNIQUE (unit_id, person_id);


--
-- TOC entry 4561 (class 2606 OID 16758)
-- Name: units units_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.units
    ADD CONSTRAINT units_pkey PRIMARY KEY (id);


--
-- TOC entry 4559 (class 2606 OID 16759)
-- Name: versioninfo versioninfo_pkey; Type: CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.versioninfo
    ADD CONSTRAINT versioninfo_pkey PRIMARY KEY (id);


--
-- TOC entry 4574 (class 1259 OID 47151)
-- Name: support_relationships_unit_id_department_id; Type: INDEX; Schema: public; Owner: itpeople_owner
--

CREATE UNIQUE INDEX support_relationships_unit_id_department_id ON public.support_relationships USING btree (unit_id, department_id);


--
-- TOC entry 4609 (class 2606 OID 16760)
-- Name: building_relationships building_relationships_building_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.building_relationships
    ADD CONSTRAINT building_relationships_building_id_fkey FOREIGN KEY (building_id) REFERENCES public.buildings(id);


--
-- TOC entry 4610 (class 2606 OID 16765)
-- Name: building_relationships building_relationships_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.building_relationships
    ADD CONSTRAINT building_relationships_unit_id_fkey FOREIGN KEY (unit_id) REFERENCES public.units(id);


--
-- TOC entry 4602 (class 2606 OID 16770)
-- Name: people people_department_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.people
    ADD CONSTRAINT people_department_id_fkey FOREIGN KEY (department_id) REFERENCES public.departments(id);


--
-- TOC entry 4603 (class 2606 OID 16775)
-- Name: support_relationships support_relationships_department_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.support_relationships
    ADD CONSTRAINT support_relationships_department_id_fkey FOREIGN KEY (department_id) REFERENCES public.departments(id);


--
-- TOC entry 4604 (class 2606 OID 16780)
-- Name: support_relationships support_relationships_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.support_relationships
    ADD CONSTRAINT support_relationships_unit_id_fkey FOREIGN KEY (unit_id) REFERENCES public.units(id);


--
-- TOC entry 4607 (class 2606 OID 16785)
-- Name: unit_member_tools unit_member_tools_membership_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_member_tools
    ADD CONSTRAINT unit_member_tools_membership_id_fkey FOREIGN KEY (membership_id) REFERENCES public.unit_members(id);


--
-- TOC entry 4608 (class 2606 OID 16790)
-- Name: unit_member_tools unit_member_tools_tool_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_member_tools
    ADD CONSTRAINT unit_member_tools_tool_id_fkey FOREIGN KEY (tool_id) REFERENCES public.tools(id);


--
-- TOC entry 4605 (class 2606 OID 16795)
-- Name: unit_members unit_members_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_members
    ADD CONSTRAINT unit_members_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.people(id);


--
-- TOC entry 4606 (class 2606 OID 16800)
-- Name: unit_members unit_members_unit_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.unit_members
    ADD CONSTRAINT unit_members_unit_id_fkey FOREIGN KEY (unit_id) REFERENCES public.units(id);


--
-- TOC entry 4601 (class 2606 OID 16805)
-- Name: units units_parent_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: itpeople_owner
--

ALTER TABLE ONLY public.units
    ADD CONSTRAINT units_parent_id_fkey FOREIGN KEY (parent_id) REFERENCES public.units(id);


--
-- TOC entry 4837 (class 0 OID 0)
-- Dependencies: 415
-- Name: TABLE audit; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.audit TO itcp_reporting;
-- GRANT ALL ON TABLE public.audit TO edb_admin;


--
-- TOC entry 4839 (class 0 OID 0)
-- Dependencies: 422
-- Name: TABLE building_relationships; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.building_relationships TO itcp_reporting;
-- GRANT ALL ON TABLE public.building_relationships TO edb_admin;


--
-- TOC entry 4841 (class 0 OID 0)
-- Dependencies: 420
-- Name: TABLE buildings; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.buildings TO itcp_reporting;
-- GRANT ALL ON TABLE public.buildings TO edb_admin;


--
-- TOC entry 4843 (class 0 OID 0)
-- Dependencies: 402
-- Name: TABLE departments; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.departments TO itcp_reporting;
-- GRANT ALL ON TABLE public.departments TO edb_admin;


--
-- TOC entry 4845 (class 0 OID 0)
-- Dependencies: 416
-- Name: TABLE historical_people; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.historical_people TO itcp_reporting;
-- GRANT ALL ON TABLE public.historical_people TO edb_admin;


--
-- TOC entry 4846 (class 0 OID 0)
-- Dependencies: 418
-- Name: TABLE hr_people; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.hr_people TO itcp_reporting;
-- GRANT ALL ON TABLE public.hr_people TO edb_admin;


--
-- TOC entry 4848 (class 0 OID 0)
-- Dependencies: 409
-- Name: TABLE logs; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.logs TO itcp_reporting;
-- GRANT ALL ON TABLE public.logs TO edb_admin;


--
-- TOC entry 4849 (class 0 OID 0)
-- Dependencies: 423
-- Name: TABLE logs_automation; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT ALL ON TABLE public.logs_automation TO edb_admin;


--
-- TOC entry 4850 (class 0 OID 0)
-- Dependencies: 404
-- Name: TABLE people; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.people TO itcp_reporting;
-- GRANT ALL ON TABLE public.people TO edb_admin;


--
-- TOC entry 4852 (class 0 OID 0)
-- Dependencies: 406
-- Name: TABLE support_relationships; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.support_relationships TO itcp_reporting;
-- GRANT ALL ON TABLE public.support_relationships TO edb_admin;


--
-- TOC entry 4854 (class 0 OID 0)
-- Dependencies: 411
-- Name: TABLE tools; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.tools TO itcp_reporting;
-- GRANT ALL ON TABLE public.tools TO edb_admin;


--
-- TOC entry 4856 (class 0 OID 0)
-- Dependencies: 413
-- Name: TABLE unit_member_tools; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.unit_member_tools TO itcp_reporting;
-- GRANT ALL ON TABLE public.unit_member_tools TO edb_admin;


--
-- TOC entry 4858 (class 0 OID 0)
-- Dependencies: 408
-- Name: TABLE unit_members; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.unit_members TO itcp_reporting;
-- GRANT ALL ON TABLE public.unit_members TO edb_admin;


--
-- TOC entry 4860 (class 0 OID 0)
-- Dependencies: 400
-- Name: TABLE units; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.units TO itcp_reporting;
-- GRANT ALL ON TABLE public.units TO edb_admin;


--
-- TOC entry 4862 (class 0 OID 0)
-- Dependencies: 398
-- Name: TABLE versioninfo; Type: ACL; Schema: public; Owner: itpeople_owner
--

-- GRANT SELECT ON TABLE public.versioninfo TO itcp_reporting;
-- GRANT ALL ON TABLE public.versioninfo TO edb_admin;


--
-- TOC entry 424 (class 1255 OID 16714)
-- -- Name: get_leaders(integer); Type: FUNCTION; Schema: public; Owner: edb_admin
--

CREATE FUNCTION public.get_leaders(integer) RETURNS text
    LANGUAGE sql
    AS $_$
	SELECT string_agg(p.netid, ', ') 
	FROM people p
	JOIN unit_members um on um.person_id = p.id
	WHERE um.unit_id = $1 AND um.role = 4 -- whatever person attribute we want to filter on
$_$;

-- -- ALTER FUNCTION public.get_leaders(integer) OWNER TO edb_admin;

--
-- TOC entry 425 (class 1255 OID 16715)
-- -- Name: unit_leaders(integer); Type: FUNCTION; Schema: public; Owner: edb_admin
--

CREATE FUNCTION public.unit_leaders(integer) RETURNS text
    LANGUAGE sql
    AS $_$
   SELECT string_agg(p.netid, ', ') 
   FROM people p
   JOIN unit_members um on um.person_id = p.id
   WHERE um.unit_id = $1 AND um.role = 4
$_$;


-- -- ALTER FUNCTION public.unit_leaders(integer) OWNER TO edb_admin;

-- Completed on 2021-01-15 10:14:02 EST

--
-- PostgreSQL database dump complete
--

");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "building_relationships",
                schema: "public");

            migrationBuilder.DropTable(
                name: "buildings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "historical_people",
                schema: "public");

            migrationBuilder.DropTable(
                name: "hr_people",
                schema: "public");

            migrationBuilder.DropTable(
                name: "unit_member_tools",
                schema: "public");

            migrationBuilder.DropTable(
                name: "people",
                schema: "public");

            migrationBuilder.DropTable(
                name: "support_relationships",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tool_permissions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tools",
                schema: "public");

            migrationBuilder.DropTable(
                name: "unit_members",
                schema: "public");

            migrationBuilder.DropTable(
                name: "units",
                schema: "public");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "public");
        }
    }
}
