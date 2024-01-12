-- A large number of spam form submissions (~1000) have been created in the PROD environment.
-- This migration will only attempt to delete the matching submissions in PROD because the form table names are different in each environment.
IF OBJECT_ID('Form_Kentico_Devnet_Form_2023_09_22_15_53', 'U') IS NOT NULL
BEGIN
    DELETE
    FROM Form_Kentico_Devnet_Form_2023_09_22_15_53
    WHERE Email = '' OR Email IS NULL OR Email = ' ' OR Email LIKE '%@example.com%'
END;