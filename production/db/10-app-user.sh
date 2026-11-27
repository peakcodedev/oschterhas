#!/bin/sh
# Runs once, when Postgres creates the database (empty volume). Creates the login of the running app:
# it may read and write the tables that the schema owner "oschterhas" (used by migrations) creates,
# but cannot create, alter, or drop anything.
set -eu

psql -v ON_ERROR_STOP=1 -v app_password="$POSTGRES_APP_PASSWORD" \
	--username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<'SQL'
CREATE ROLE oschterhas_app LOGIN PASSWORD :'app_password';
GRANT CONNECT ON DATABASE oschterhas TO oschterhas_app;
GRANT USAGE ON SCHEMA public TO oschterhas_app;
ALTER DEFAULT PRIVILEGES FOR ROLE oschterhas IN SCHEMA public
	GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO oschterhas_app;
ALTER DEFAULT PRIVILEGES FOR ROLE oschterhas IN SCHEMA public
	GRANT USAGE, SELECT ON SEQUENCES TO oschterhas_app;
SQL
