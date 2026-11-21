CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117011805_InitialCreate') THEN
    CREATE TABLE departments (
        id integer NOT NULL,
        code character varying(10) NOT NULL,
        name character varying(100) NOT NULL,
        CONSTRAINT pk_departments PRIMARY KEY (id),
        CONSTRAINT ak_departments_code UNIQUE (code)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117011805_InitialCreate') THEN
    CREATE TABLE employees (
        id integer NOT NULL,
        name character varying(100) NOT NULL,
        department_code character varying(10) NOT NULL,
        email character varying(200),
        manager_id integer,
        joined_on date NOT NULL,
        location character varying(20) NOT NULL,
        monthly_salary numeric(12,2) NOT NULL,
        employee_type character varying(20) NOT NULL,
        address_city character varying(100),
        address_pincode character varying(10),
        CONSTRAINT pk_employees PRIMARY KEY (id),
        CONSTRAINT fk_employees_departments_department_code FOREIGN KEY (department_code) REFERENCES departments (code) ON DELETE RESTRICT,
        CONSTRAINT fk_employees_employees_manager_id FOREIGN KEY (manager_id) REFERENCES employees (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117011805_InitialCreate') THEN
    CREATE INDEX ix_employees_department_code ON employees (department_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117011805_InitialCreate') THEN
    CREATE INDEX ix_employees_manager_id ON employees (manager_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117011805_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261117011805_InitialCreate', '10.0.15');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261118234749_AddUsers') THEN
    CREATE TABLE users (
        id integer NOT NULL,
        email character varying(200) NOT NULL,
        password_hash character varying(200) NOT NULL,
        role character varying(20) NOT NULL,
        created_on timestamp with time zone NOT NULL,
        CONSTRAINT pk_users PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261118234749_AddUsers') THEN
    CREATE UNIQUE INDEX ix_users_email ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261118234749_AddUsers') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261118234749_AddUsers', '10.0.15');
    END IF;
END $EF$;
COMMIT;

