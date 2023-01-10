using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    /// <inheritdoc />
    public partial class UserSearchAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_search_attributes",
                columns: table => new
                {
                    usersearchattributeid = table.Column<Guid>(name: "user_search_attribute_id", type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    userid = table.Column<Guid>(name: "user_id", type: "uuid", nullable: false),
                    attributetype = table.Column<string>(name: "attribute_type", type: "text", nullable: false),
                    attributevalue = table.Column<string>(name: "attribute_value", type: "text", nullable: false, collation: "case_insensitive")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_search_attributes", x => x.usersearchattributeid);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_search_attributes_attribute_type_and_value",
                table: "user_search_attributes",
                columns: new[] { "attribute_type", "attribute_value" });

            migrationBuilder.CreateIndex(
                name: "ix_user_search_attributes_user_id",
                table: "user_search_attributes",
                column: "user_id");

            var dataPopulationSql = @"
INSERT INTO
    user_search_attributes
    (
        user_id,
        attribute_type,
        attribute_value
    )
SELECT
    user_id,
    attribute_type,
    CASE 
        WHEN attribute_type = 'first_name' THEN first_name
        WHEN attribute_type = 'last_name' THEN last_name
        WHEN attribute_type = 'date_of_birth' THEN to_char(date_of_birth, 'yyyy-mm-dd')
        WHEN attribute_type = 'trn' THEN trn
    END attribute_value
FROM
    (SELECT
        attribute_type,
        u.*
    FROM
        users u
    CROSS JOIN
        unnest(ARRAY['first_name','last_name','date_of_birth','trn']) attribute_type) u
WHERE
    u.is_deleted = false
    AND ((attribute_type = 'first_name' AND first_name IS NOT NULL)
         OR (attribute_type = 'last_name' AND last_name IS NOT NULL)
         OR (attribute_type = 'date_of_birth' AND date_of_birth IS NOT NULL)
         OR (attribute_type = 'trn' AND trn IS NOT NULL))
";
            migrationBuilder.Sql(dataPopulationSql);

            var triggerFunctionSql = @"
CREATE OR REPLACE FUNCTION fn_update_user_search_attributes()
    RETURNS trigger
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    IF ((TG_OP = 'DELETE') OR (TG_OP = 'UPDATE')) THEN
        DELETE FROM
            user_search_attributes
        WHERE
            user_id = OLD.user_id;
    END IF;
    
    IF (((TG_OP = 'INSERT') OR (TG_OP = 'UPDATE')) AND NEW.is_deleted IS false) THEN
        INSERT INTO
            user_search_attributes
            (
                user_id,
                attribute_type,
                attribute_value
            )
        VALUES
            (
                NEW.user_id,
                'first_name',
                NEW.first_name
            );
        
        INSERT INTO
            user_search_attributes
            (
                user_id,
                attribute_type,
                attribute_value
            )
        VALUES
            (
                NEW.user_id,
                'last_name',
                NEW.last_name
            );

        IF (NEW.date_of_birth IS NOT NULL) THEN
            INSERT INTO
                user_search_attributes
                (
                    user_id,
                    attribute_type,
                    attribute_value
                )
            VALUES
                (
                    NEW.user_id,
                    'date_of_birth',
                    to_char(NEW.date_of_birth, 'yyyy-mm-dd')
                );
        END IF;

        IF (NEW.trn IS NOT NULL) THEN
            INSERT INTO
                user_search_attributes
                (
                    user_id,
                    attribute_type,
                    attribute_value
                )
            VALUES
                (
                    NEW.user_id,
                    'trn',
                    NEW.trn
                );
        END IF;	
    END IF;
    
    RETURN NULL; -- result is ignored since this is an AFTER trigger
END;
$BODY$
";
            migrationBuilder.Sql(triggerFunctionSql);

            var triggerSql = @"
CREATE TRIGGER trg_update_user_search_attributes
    AFTER INSERT OR DELETE OR UPDATE OF first_name, last_name, date_of_birth, trn, is_deleted
    ON users
    FOR EACH ROW
    EXECUTE FUNCTION fn_update_user_search_attributes()
";
            migrationBuilder.Sql(triggerSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_search_attributes");
        }
    }
}
