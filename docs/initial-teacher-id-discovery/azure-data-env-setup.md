# Azure set-up
## Elevate access
- Navigate to portal.azure.com
- Navigate to Privileged Identity Management 
- Click My roles
- Click Azure resources
- Look for s106-applyforpostgraduateteachertraining-production
- Click activate
- Others in the managers group will receive an email for approval
- One of them should click and approve
- Create resource group
    - s106p01-teacherid-data-stage
    - Region: West Europe
    - Tags:
    - Environment:Prod
    - Portfolio:Early Years and Schools Group
    - Product:Apply for postgraduate teacher training
    - Service:Teacher services
    - Service Line:Teaching Workforce

## Create postgres server
- Navigate to Azure Database for PostgreSQL server
- Create single server
- s106p01-teacherid-data-stage-postgres
- Choose resource group above
- Location: West Europe
- Version: 11
- Plan: Basic, 1 vCore(s), 50 GB
- Admin username: admintsdata
- Password: <keep it safe>
- Allow connection from app service
- In postgres, select connection security
- Select Yes to Allow access to Azure services

## Create app service
- Navigate to App Services
- Create
- subscription:s106-applyforpostgraduateteachertraining-- production
- App name: s106p01-teacherid-data-stage-pgadmin
- Choose resource group above
- Publish: Docker container
- OS: Linux
- Region: West Europe
- Linux plan: Dev/Test 100 total ACU
- Docker:
- Single container
- Image source: Docker Hub
- Access type: Public
- Image ang tag: dpage/pgadmin4:5

- Configure app service
- Based on - https://www.pgadmin.org/docs/pgadmin4/latest/container_deployment.html
- Go to resource
- Configuration
- New application setting:
- PGADMIN_CONFIG_ENHANCED_COOKIE_PROTECTION: False
- PGADMIN_DEFAULT_EMAIL: <admin user email>
- PGADMIN_DEFAULT_PASSWORD: <admin user password>
- Click Save
- Add postgres server to pgadmin
- Navigate to pgadmin (URL in app service overview)
- Connect with pgadmin user
- Add server
- Host: read from posgres server connection strings
- User: read from posgres server connection strings
- Password: DB admin password created above
- Select Shared


## Load data via a .csv file (e.g. DQT)

- Install Postgresql on a developer pc
- For Mac: brew install postgresql@12
- Extract data in .csv format to your developer pc
- Create a database
- Create the required table in your postgresql environment to load .csv [see current contact entity extract table DDL below here]
- Add your IP in postgres server connection security (remove it after use)
- From a bash shell on your development pc load the data with the following command:
    - cat [path] | psql "host=s106p01-teacherid-data-stage-postgres.postgres.database.azure.com port=5432 dbname=[dbname] user=admintsdata@s106p01-teacherid-data-stage-postgres password=[pword] sslmode=require" -c "COPY contact FROM STDIN WITH DELIMITER ',' NULL 'NULL' CSV;"


## Teaching vacancies (AWS)
- Get Data

- https://teaching-vacancies.signin.aws.amazon.com/console
Switch role
Account 530003481352
Deployments
full: https://s3.console.aws.amazon.com/s3/buckets/530003481352-tv-db-backups?region=eu-west-2&prefix=full/&showversions=false
sanitised: https://s3.console.aws.amazon.com/s3/buckets/530003481352-tv-db-backups?region=eu-west-2&prefix=sanitised/&showversions=false

## Create database
- In PGAdmin right click on server - create database [dbname]
OR
- Install Postgresql on a developer pc
For Mac: brew install postgresql@12
In psql 	connect to postgresql server: 
psql "host=[host] port=5432 dbname=[dbnameToCreate] user=[user] password=[pword] sslmode=require" 
create database[name]
- Load TV data
Add your IP in postgres server connection security (remove it after use)
psql command:
psql "host=s106p01-teacherid-data-stage-postgres.postgres.database.azure.com port=5432 dbname={your_database} user=admintsdata@s106p01-teacherid-data-stage-postgres password={your_password} sslmode=require" < [nameofsqlfile]


## Apply (GOVPaaS)
- Install cf cli
- Install cf cli v7: brew install cloudfoundry/tap/cf-- cli@7
- Install conduit plugin: cf install-plugin conduit

- Dump Apply database
- Login to paas
- Target space: cf target -s bat-prod
- Dump database:
- cf conduit apply-postgres-prod -- pg_dump -E utf8 --clean --if-exists --no-owner --verbose --no-password -f apply.sql

- Create Apply database

- Create in PgAdmin: right click on server - create database
Or from command: 


- OR use the Apply support tool to extract just the data required:

- https://www.apply-for-teacher-training.service.gov.uk/support/applications
Then use https://www.apply-for-teacher-training.service.gov.uk/support/blazer
To download a subset as .csv extract load in through psql from a terminal:


## SQL Env create:


--*******************************************************************************************************
--* Script to help create a data analysis database in a postgreSQL server
--* Requires: psql, terminal, access to data files / back ups
--* Environments:
--* Production:
--* host=s106p01-teacherid-data-stage-postgres.postgres.database.azure.com
--* user=admintsdata@s106p01-teacherid-data-stage-postgres
--* Development:
--*
--* Data:
--* Contains the following data sources: [DQT.Contact entity, TVS.full db from back up,
--* Apply.application_forms, Apply.candidates]
--*******************************************************************************************************
 
 
--*******************************************************************************************************
-- Create database to perform analysis
--*******************************************************************************************************
-- Database: smash
-- DROP DATABASE smash;
 
CREATE DATABASE smash
   WITH
   OWNER = admintsdata
   ENCODING = 'UTF8'
   LC_COLLATE = 'English_United States.1252'
   LC_CTYPE = 'English_United States.1252'
   TABLESPACE = pg_default
   CONNECTION LIMIT = -1;
 
 
--*******************************************************************************************************
-- Create DQT tables
--*******************************************************************************************************
--create contact table (cut down version) dqt
 
-- Table: public.contact
-- DROP TABLE public.contact;
CREATE TABLE IF NOT EXISTS public.dqt_contact
(
   dfeta_title character varying(100) COLLATE pg_catalog."default",
   firstname character varying(100) COLLATE pg_catalog."default",
   middlename character varying(100) COLLATE pg_catalog."default",
   lastname character varying(100) COLLATE pg_catalog."default",
   dfeta_previouslastname character varying(50) COLLATE pg_catalog."default",
   yomifullname character varying(450) COLLATE pg_catalog."default",
   fullname character varying(160) COLLATE pg_catalog."default",
   birthdate timestamp without time zone,
   dfeta_trn character varying(50) COLLATE pg_catalog."default",
   dfeta_ninumber character varying(60) COLLATE pg_catalog."default",
   dfeta_trnrequired bit(1),
   address1_line1 character varying(250) COLLATE pg_catalog."default",
   address1_line2 character varying(250) COLLATE pg_catalog."default",
   address1_line3 character varying(250) COLLATE pg_catalog."default",
   address1_city character varying(80) COLLATE pg_catalog."default",
   address1_postalcode character varying(20) COLLATE pg_catalog."default",
   address1_telephone1 character varying(50) COLLATE pg_catalog."default",
   emailaddress1 character varying(100) COLLATE pg_catalog."default",
   emailaddress2 character varying(100) COLLATE pg_catalog."default",
   emailaddress3 character varying(100) COLLATE pg_catalog."default",
   dfeta_previousemail character varying(200) COLLATE pg_catalog."default",
   mobilephone character varying(60) COLLATE pg_catalog."default",
   telephone1 character varying(60) COLLATE pg_catalog."default",
   jobtitle character varying(100) COLLATE pg_catalog."default",
   dfeta_qtsdate timestamp without time zone,
   dfeta_eytsdate timestamp without time zone,
   dfeta_inductionstatus integer,
   id uuid NOT NULL,
   dfeta_husid character varying(100) COLLATE pg_catalog."default",
   dfeta_tssupdate timestamp without time zone,
   CONSTRAINT pk1 PRIMARY KEY (id)
)
WITH (
   OIDS = FALSE
)
TABLESPACE pg_default;
ALTER TABLE public.dqt_contact
   OWNER to admintsdata;
 
--*******************************************************************************************************
-- Load DQT data
-- pre-requisite: a csv data file matching table def
-- Please refer to the set up doc for instructions on how to get data:
-- https://docs.google.com/document/d/1o_CHrGRMKACngJC4_3hZMFRYexGOcw9Lex--9wBMhJk/edit?usp=sharing
--*******************************************************************************************************
 
--run from psql terminal
cat [path/file] | psql "host=s106p01-teacherid-data-stage-postgres.postgres.database.azure.com port=5432 dbname=dqt user=admintsdata@s106p01-teacherid-data-stage-postgres password=[password] sslmode=require" -c "COPY [destination tablename] FROM STDIN WITH DELIMITER ',' NULL 'NULL' CSV;"
 
--*******************************************************************************************************
-- Create Indexes
--*******************************************************************************************************
-- DROP INDEX public."IX_EmailAddress1";
 
CREATE INDEX "IX_EmailAddress1"
   ON public.dqt_contact USING btree
   (emailaddress1 COLLATE pg_catalog."default" ASC NULLS LAST)
   TABLESPACE pg_default;
-- Index: IX_EmailAddress2
 
-- DROP INDEX public."IX_EmailAddress2";
 
CREATE INDEX "IX_EmailAddress2"
   ON public.dqt_contact USING btree
   (emailaddress2 COLLATE pg_catalog."default" ASC NULLS LAST)
   TABLESPACE pg_default;
-- Index: IX_EmailAddress3
 
-- DROP INDEX public."IX_EmailAddress3";
 
CREATE INDEX "IX_EmailAddress3"
   ON public.dqt_contact USING btree
   (emailaddress3 COLLATE pg_catalog."default" ASC NULLS LAST)
   TABLESPACE pg_default;
-- Index: IX_NINO
 
-- DROP INDEX public."IX_NINO";
 
CREATE INDEX "IX_NINO"
   ON public.dqt_contact USING btree
   (dfeta_ninumber COLLATE pg_catalog."default" ASC NULLS LAST)
   TABLESPACE pg_default;
 
--*******************************************************************************************************
-- Teacher Vacancies
-- Please refer to the set up doc for instructions on how to get data:
-- https://docs.google.com/document/d/1o_CHrGRMKACngJC4_3hZMFRYexGOcw9Lex--9wBMhJk/edit?usp=sharing
--*******************************************************************************************************
 
-- From backup, use psql to create a db...
psql "host=s106dteacheriddataserver.postgres.database.azure.com port=5432 dbname=[enter db name] user=admintsdata@s106dteacheriddataserver password=[password] sslmode=require" < 2021-09-28-02-30-39-sanitised.sql
 
 
 
 
--*******************************************************************************************************
-- Apply
-- Please refer to the set up doc for instructions on how to get data:
-- https://docs.google.com/document/d/1o_CHrGRMKACngJC4_3hZMFRYexGOcw9Lex--9wBMhJk/edit?usp=sharing
--*******************************************************************************************************
 
-- Table: public.apply_application_forms
 
-- DROP TABLE public.apply_application_forms;
 
CREATE TABLE IF NOT EXISTS public.apply_application_forms
(
   id bigint NOT NULL,
   candidate_id bigint NOT NULL,
   first_name character varying COLLATE pg_catalog."default",
   last_name character varying COLLATE pg_catalog."default",
   date_of_birth text COLLATE pg_catalog."default",
   phone_number character varying COLLATE pg_catalog."default",
   address_line1 character varying COLLATE pg_catalog."default",
   address_line2 character varying COLLATE pg_catalog."default",
   address_line3 character varying COLLATE pg_catalog."default",
   address_line4 character varying COLLATE pg_catalog."default",
   country character varying COLLATE pg_catalog."default",
   postcode character varying COLLATE pg_catalog."default",
   CONSTRAINT application_forms_pkey PRIMARY KEY (id)
)
WITH (
   OIDS = FALSE
)
TABLESPACE pg_default;
 
ALTER TABLE public.apply_application_forms
   OWNER to admintsdata;
-- Index: index_application_forms_on_candidate_id
 
-- DROP INDEX public.index_application_forms_on_candidate_id;
 
CREATE INDEX index_application_forms_on_candidate_id
   ON public.apply_application_forms USING btree
   (candidate_id ASC NULLS LAST)
   TABLESPACE pg_default;
 
 
-- Table: public.apply_candidates
 
-- DROP TABLE public.apply_candidates;
 
CREATE TABLE IF NOT EXISTS public.apply_candidates
(
   id bigint NOT NULL,
   email_address character varying COLLATE pg_catalog."default" NOT NULL,
   CONSTRAINT candidates_pkey PRIMARY KEY (id)
)
WITH (
   OIDS = FALSE
)
TABLESPACE pg_default;
 
ALTER TABLE public.apply_candidates
   OWNER to admintsdata;
-- Index: index_candidates_on_email_address
 
-- DROP INDEX public.index_candidates_on_email_address;
 
CREATE UNIQUE INDEX index_candidates_on_email_address
   ON public.apply_candidates USING btree
   (email_address COLLATE pg_catalog."default" ASC NULLS LAST)
   TABLESPACE pg_default;
 
 
-- Get data (to match table defs)
-- see
 
cat /Users/aje/Downloads/query.csv | psql "host=s106p01-teacherid-data-stage-postgres.postgres.database.azure.com port=5432 dbname=smash user=admintsdata@s106p01-teacherid-data-stage-postgres password=[password]] sslmode=require" -c "COPY apply_application_forms FROM STDIN WITH DELIMITER ',' NULL 'NULL' CSV;"
 
https://docs.google.com/document/d/1o_CHrGRMKACngJC4_3hZMFRYexGOcw9Lex--9wBMhJk/edit?usp=sharing


