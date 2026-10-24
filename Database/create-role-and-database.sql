\getenv rolename SB_ROLE
\getenv dbname SB_DB
\getenv apppassword SB_APP_PASSWORD

\set ON_ERROR_STOP on

SELECT count(*) > 0 AS role_exists FROM pg_roles WHERE rolname = :'rolename' \gset
\if :role_exists
\echo Rolli ekziston tashme, kalohet te baza
\else
CREATE ROLE :"rolename" LOGIN PASSWORD :'apppassword';
\endif

SELECT count(*) > 0 AS database_exists FROM pg_database WHERE datname = :'dbname' \gset
\if :database_exists
\echo Baza ekziston tashme, riparoj te drejtat
\else
CREATE DATABASE :"dbname" OWNER :"rolename";
\endif

\c :dbname

GRANT ALL ON SCHEMA public TO :"rolename";
GRANT ALL ON ALL TABLES IN SCHEMA public TO :"rolename";
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO :"rolename";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO :"rolename";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO :"rolename";

SELECT current_database() AS baza, :'rolename' AS roli;
