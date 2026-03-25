CREATE VIEW [dbo].[UsersForGetUsersByFilter]
AS
SELECT
    CAST([UserId] AS INT) AS [UserId],
    CAST([UserName] AS NVARCHAR(200)) AS [DisplayName],
    [Email]
FROM [dbo].[Users];
GO
