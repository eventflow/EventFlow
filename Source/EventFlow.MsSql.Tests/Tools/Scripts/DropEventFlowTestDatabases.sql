-- Drop all databases that start with TEST__
use master
go
declare @dbnames nvarchar(max)
declare @statement nvarchar(max)
set @dbnames = ''
set @statement = ''
select @dbnames = @dbnames + ',[' + name + ']' from sys.databases where name like 'TEST__%'
if len(@dbnames) = 0
    begin
    print 'no databases to drop'
    end
else
    begin
    set @statement = 'drop database ' + substring(@dbnames, 2, len(@dbnames))
    print @statement
    exec sp_executesql @statement
    end