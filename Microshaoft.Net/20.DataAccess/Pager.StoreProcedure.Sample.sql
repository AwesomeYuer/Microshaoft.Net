USE [MessagesPush]
GO
/****** Object:  StoredProcedure [dbo].[zsp_GetAppGroupUsers]    Script Date: 2014/4/27 13:46:34 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER proc [dbo].[zsp_GetAppGroupUsers]
@AppID varchar(50)
, @GroupID varchar(50)
, @UserID varchar(50) = '*'
, @OffsetRows int
, @FetchRows int
, @TotalRows int = -1 out
, @IsLast bit = 0 out

as
begin
/*
	declare @OffsetRows int = 0
	declare @FetchRows int = 1
	declare @islast bit = 0
	declare @TotalRows int = -1
	exec zsp_GetAppGroupUsers
				'cos'
				, 'audit'
				, '*'
				, @OffsetRows
				, @FetchRows
				, @TotalRows out
				, @islast out
	select @TotalRows, @OffsetRows, @FetchRows, @islast 
*/

	if (@TotalRows <= 0)
	begin
		select
			@TotalRows = count(1)
		from
			[AppsGroupsUsers]
		where
		(AppID = @AppID or @AppID ='*' )
		and (GroupID = @GroupID or @GroupID ='*' )
		and(@UserID = '*' or UserID = @UserID)
	end
	if (@TotalRows <= (@OffsetRows + @FetchRows))
	begin
		set @Islast = 1
	end
	
	SELECT
		*
	from
		[AppsGroupsUsers]
	where
		(AppID = @AppID or @AppID ='*' )
		and (GroupID = @GroupID or @GroupID ='*' )
		and(@UserID = '*' or UserID = @UserID)
	order by
		ID
	OFFSET
		@OffsetRows
					ROWS 
	FETCH NEXT
		@FetchRows
			ROWS ONLY
end 