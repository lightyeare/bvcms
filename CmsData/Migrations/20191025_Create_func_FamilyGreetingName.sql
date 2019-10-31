IF OBJECT_ID('[dbo].[FamilyGreetingName]') IS NOT NULL
DROP FUNCTION [dbo].[FamilyGreetingName]
GO
CREATE FUNCTION [dbo].[FamilyGreetingName]
(
    @type int = 0 -- 0 = Formal, 1 = Informal full names, 2 = Informal First Names
    , @peopleId int
)
RETURNS nvarchar(MAX)
AS
BEGIN
    DECLARE @Result nvarchar(MAX) = NULL;

    DECLARE @familyId int
    , @hohId int
    , @spouseId int
    , @hohFirst nvarchar(100)
    , @hohLast nvarchar(100)
    , @hohTitle nvarchar(10)
    , @spouseFirst nvarchar(100)
    , @spouseLast nvarchar(100)
    , @spouseTitle nvarchar(10)
    , @customGreeting nvarchar(MAX)
    , @contributeIndividually int

    SELECT @familyId = p.FamilyId
        , @hohId = CASE p.EnvelopeOptionsId
        WHEN 1 THEN p.PeopleId
        ELSE hoh.PeopleId
    END
        , @spouseId = sp.PeopleId
        , @hohFirst = CASE p.EnvelopeOptionsId
        WHEN 1 THEN p.PreferredName
        ELSE hoh.PreferredName
    END
        , @hohLast = CASE p.EnvelopeOptionsId
        WHEN 1 THEN p.LastName
        ELSE hoh.LastName
    END
        , @hohTitle = CASE p.EnvelopeOptionsId
        WHEN 1 THEN CASE WHEN (p.GenderId = 1 AND p.TitleCode IS NULL)
        THEN 'Mr.'
        WHEN (p.GenderId = 2 AND p.TitleCode IS NULL)
        THEN CASE WHEN p.MaritalStatusId = 20
        THEN 'Mrs.'
        ELSE 'Ms.'
    END
    WHEN (p.GenderId = 0 AND p.TitleCode IS NULL)
        THEN NULL
        ELSE p.TitleCode
    END
    ELSE CASE WHEN (hoh.GenderId = 1 AND hoh.TitleCode IS NULL)
        THEN 'Mr.'
        WHEN (hoh.GenderId = 2 AND hoh.TitleCode IS NULL)
        THEN CASE WHEN hoh.MaritalStatusId = 20
        THEN 'Mrs.'
        ELSE 'Ms.'
    END
        WHEN (hoh.GenderId = 0 AND hoh.TitleCode IS NULL)
        THEN NULL
        ELSE hoh.TitleCode
        END
    END
        , @spouseFirst = sp.PreferredName
        , @spouseLast = sp.LastName
        , @spouseTitle = CASE WHEN (sp.GenderId = 1 AND sp.TitleCode IS NULL)
        THEN 'Mr.'
        WHEN (sp.GenderId = 2 AND sp.TitleCode IS NULL)
        THEN CASE WHEN sp.MaritalStatusId = 20
        THEN 'Mrs.'
        ELSE 'Ms.'
    END
    WHEN (sp.GenderId = 0 AND sp.TitleCode IS NULL)
        THEN NULL
        ELSE sp.TitleCode
    END
        , @customGreeting = fe.[Data]
        , @contributeIndividually = p.EnvelopeOptionsId
    FROM dbo.People p
    INNER JOIN dbo.Families f ON p.FamilyId = f.FamilyId
    LEFT JOIN dbo.FamilyExtra fe ON f.FamilyId = fe.FamilyId AND fe.Field = 'CoupleName'
    LEFT JOIN dbo.People hoh ON f.HeadOfHouseholdId = hoh.PeopleId
    LEFT JOIN dbo.People sp ON f.HeadOfHouseholdSpouseId = sp.PeopleId
    WHERE p.PeopleId = @peopleId;

    -- This function will always prefer the CoupleName option
    SELECT @Result = COALESCE(@customGreeting,(
        SELECT
        CASE
            WHEN @type = 0
            THEN
            CASE
                WHEN @spouseId IS NULL OR @contributeIndividually = 1
                THEN
                CASE
                    WHEN @HoHTitle IS NOT NULL
                    THEN @HoHTitle + ' ' + @HoHFirst + ' ' + @HoHLast
                    ELSE @HoHFirst + ' ' + @HoHLast
                    END
                WHEN @spouseId IS NOT NULL AND @contributeIndividually <> 1
                THEN
            CASE
                WHEN @HoHTitle IS NOT NULL AND @SpouseTitle IS NOT NULL
                THEN
            CASE
                WHEN @HoHLast <> @SpouseLast
                THEN @HoHTitle + ' ' + @HoHFirst + ' ' + @HoHLast + ' and ' + @SpouseTitle + ' ' + @SpouseFirst + ' ' + @SpouseLast
                ELSE CASE
                    WHEN @SpouseTitle <> 'Mrs.' AND @SpouseTitle <> 'Ms.'
                    THEN @HoHTitle + ' ' + @HoHFirst + ' and ' + @spouseTitle + ' ' + @spouseFirst + ' ' + @spouseLast
                    ELSE @HoHTitle + ' and ' + @SpouseTitle + ' ' + @HoHFirst + ' ' + @HoHLast
                END
            END
            ELSE CASE --This case is only used if one OR both adults have an unknown gender AND no title.
                WHEN @HoHLast <> @SpouseLast
                THEN @HoHFirst + ' ' + @HoHLast + ' and ' + @SpouseFirst + ' ' + @SpouseLast
                ELSE @HoHFirst + ' and ' + @SpouseFirst + ' ' + @HoHLast
                END
            END
        END
        WHEN @type = 1
        THEN CASE
            WHEN @spouseId IS NULL OR @contributeIndividually = 1
            THEN @HoHFirst + ' ' + @HoHLast
            WHEN @HoHLast <> @SpouseLast
            THEN @HoHFirst + ' ' + @HoHLast + ' and ' + @SpouseFirst + ' ' + @SpouseLast
            ELSE @HoHFirst + ' and ' + @SpouseFirst + ' ' + @HoHLast
        END
        ELSE CASE
            WHEN @spouseId IS NULL OR @contributeIndividually = 1
            THEN @HoHFirst
            ELSE @HoHFirst + ' and ' + @SpouseFirst
            END
        END
    ))

    -- Return the result of the function
    RETURN @Result

END
GO
