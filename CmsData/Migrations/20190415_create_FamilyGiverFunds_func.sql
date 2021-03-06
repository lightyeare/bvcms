DROP FUNCTION IF EXISTS [dbo].[FamilyGiverFunds]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[FamilyGiverFunds](@fd DATETIME, @td DATETIME, @funds varchar(max))
RETURNS TABLE 
AS
RETURN 
(
WITH units AS ( 
	SELECT c.FamilyId
           ,SUM(c.Amount) Amount
           ,SUM(c.PledgeAmount) Pledge
	FROM     dbo.Contributions2(@fd, @td, 0, 1, NULL, 1) c
	WHERE FundId in (select value from dbo.SplitInts(@funds))
	GROUP BY c.FamilyId
)
SELECT  p.FamilyId
       ,p.PeopleId
       ,FamGive = CAST(IIF(u.Amount > 0, 1, 0) AS BIT)
       ,FamPledge = CAST(IIF(u.Pledge > 0, 1, 0) AS BIT)
FROM dbo.People p
LEFT JOIN units u ON u.FamilyId = p.FamilyId
)
GO
