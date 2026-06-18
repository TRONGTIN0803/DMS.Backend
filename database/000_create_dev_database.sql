DO
$$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'dms_app') THEN
        CREATE ROLE dms_app LOGIN PASSWORD 'dms_password';
    ELSE
        ALTER ROLE dms_app WITH LOGIN PASSWORD 'dms_password';
    END IF;
END
$$;

SELECT 'CREATE DATABASE dms OWNER dms_app'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'dms')\gexec

GRANT ALL PRIVILEGES ON DATABASE dms TO dms_app;

