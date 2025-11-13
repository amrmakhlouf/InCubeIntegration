IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE name = 'CalculateDistance' AND type = 'FN')
BEGIN

EXEC('CREATE function [dbo].[CalculateDistance](@Lat1 decimal(19,9),@Long1 decimal(19,9),@Lat2 decimal(19,9),@Long2 decimal(19,9),@DigitsNumber int)
returns nvarchar(50)
as
begin

IF (@Lat1 = 0 OR @Long1 = 0 OR @Lat2 = 0 OR @Long2 = 0)
RETURN 0;

declare @R int
declare  @Phi1 decimal(20,18)
declare  @Phi2 decimal(20,18)
declare  @dPhi decimal(20,18)
declare  @dLambda decimal(20,18)
declare  @a decimal(30,28)
declare  @c decimal(30,20)
set @R = 6371000;
set @Phi1 = @lat1 * PI() / 180
set @Phi2 = @lat2 * PI() / 180
set @dPhi = (@lat2-@lat1)*PI()/180
set @dLambda = (@Long2-@Long1)*PI()/180

set @a = sin(@dPhi/2) * sin(@dPhi/2) +
        cos(@Phi1) * cos(@Phi2) *
        sin(@dLambda/2) * sin(@dLambda/2);
set @c = 2 * @r * atn2(sqrt(@a), sqrt(1-@a));

set @c = ROUND(@c,@DigitsNumber)
declare @int nvarchar(20)
set @int = cast(cast(@c as int) as nvarchar(20))
set @DigitsNumber = @DigitsNumber + case @DigitsNumber when 0 then -1 else 1 end
return left(cast(@c as nvarchar(50)) + ''0000000000000'',len(@int)+1+@digitsnumber)

end')

END

IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE name = 'FormatGeoCodes' AND type = 'FN')
BEGIN

EXEC('CREATE function [dbo].[FormatGeoCodes](@Lat decimal(19,9),@Long decimal(19,9),@DigitsNumber int)
returns nvarchar(50)
as
begin

return    (SELECT LEFT(CAST(ROUND(ISNULL(@Lat,0),@DigitsNumber) AS nvarchar(20)) + CASE ISNULL(@Lat,0) WHEN 0 THEN '''' ELSE ''0000000000000'' END,@DigitsNumber+3) 
+ '' , '' 
+ LEFT(CAST(ROUND(ISNULL(@Long,0),@DigitsNumber) AS nvarchar(20)) + CASE ISNULL(@Long,0) WHEN 0 THEN '''' ELSE ''0000000000000'' END,@DigitsNumber+3))
end')

END

IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE name = 'RepresentTime' AND type = 'FN')
BEGIN

EXEC('create function [dbo].[RepresentTime](@TimeInSeconds int,@ShowHours bit, @ShowMinutes bit, @ShowSeconds bit)
returns nvarchar(50)
as
begin

declare @Time nvarchar(20)
declare @hours int, @minutes int, @seconds int
set @Time = ''''
if (@ShowHours = 1)
begin
set @hours = @TimeInSeconds / 3600
set @Time = cast(@hours as nvarchar(10))
set @TimeInSeconds = @TimeInSeconds - (3600 * @hours)
end

if (@ShowMinutes = 1)
begin
set @minutes = @TimeInSeconds / 60
set @Time = @Time + (case @Time when '''' then '''' else '':'' end) + (case len(cast(@minutes as nvarchar(10))) when 1 then ''0'' else '''' end) + cast(@minutes as nvarchar(10))
set @TimeInSeconds = @TimeInSeconds - (60 * @minutes)
end

if (@ShowSeconds = 1)
begin
set @Time = @Time + (case @Time when '''' then '''' else '':'' end) + (case len(cast(@TimeInSeconds as nvarchar(10))) when 1 then ''0'' else '''' end) + cast(@TimeInSeconds as nvarchar(10))
end

return @Time

end')

END
