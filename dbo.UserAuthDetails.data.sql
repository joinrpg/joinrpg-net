--INSERT INTO [dbo].[UserAuthDetails] ([UserId], [EmailConfirmed], [RegisterDate], [IsAdmin]) VALUES (newid(), test@test.ru, getdate(), 1)
select * from [dbo].[UserAuthDetails]
--update top(1) [dbo].[UserAuthDetails]
--set EmailConfirmed=1,isadmin=1
--where userId=1

select * from Projects