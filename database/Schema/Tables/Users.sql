-- Hand-authored placeholder table for v1 skeleton
CREATE TABLE [dbo].[Users]
(
    [UserId] BIGINT NOT NULL PRIMARY KEY,
    [UserName] NVARCHAR(256) NOT NULL,
    [Email] NVARCHAR(512) NULL
);