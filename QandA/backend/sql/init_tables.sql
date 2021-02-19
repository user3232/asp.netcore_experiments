-- https://github.com/PacktPublishing/ASP.NET-Core-3-and-React/blob/master/Chapter07/backend/SQLScripts/01-Tables.sql

-- switch to QandA context
USE QandA
GO



SET IDENTITY_INSERT dbo.Question ON -- Allows explicit values to be inserted 
                                    -- into the identity column of a table.
GO
INSERT INTO dbo.Question(QuestionId, Title, Content, UserId, UserName, Created)
VALUES(1, 'Why should I learn TypeScript?', 
		'TypeScript seems to be getting popular so I wondered whether it is worth my time learning it? What benefits does it give over JavaScript?',
		'1',
		'bob.test@test.com',
		'2019-05-18 14:32')

INSERT INTO dbo.Question(QuestionId, Title, Content, UserId, UserName, Created)
VALUES(2, 'Which state management tool should I use?', 
		'There seem to be a fair few state management tools around for React - React, Unstated, ... Which one should I use?',
		'2',
		'jane.test@test.com',
		'2019-05-18 14:48')
GO
SET IDENTITY_INSERT dbo.Question OFF
GO



SET IDENTITY_INSERT dbo.Answer ON 
GO
INSERT INTO dbo.Answer(AnswerId, QuestionId, Content, UserId, UserName, Created)
VALUES(1, 1, 'To catch problems earlier speeding up your developments', '2', 'jane.test@test.com', '2019-05-18 14:40')

INSERT INTO dbo.Answer(AnswerId, QuestionId, Content, UserId, UserName, Created)
VALUES(2, 1, 'So, that you can use the JavaScript features of tomorrow, today', '3', 'fred.test@test.com', '2019-05-18 16:18')
GO
SET IDENTITY_INSERT dbo.Answer OFF 
GO