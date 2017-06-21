Do you need to reduce SQL Injection attacks, or do you need to quickly parameterize your SqlCommand object so you can take advantage of SQL Server 2016's Always Encrypted? Then SQLCommandFilters is the project for you.

SQLCommandFilters is a simple assembly that examines the text in your SqlCommand object and by using the T-SQL Parser creates an auto-parameterized version of your object.

Usage:
using SqlCommandFilters;
SqlCommandFilters.Parameters.Parameterize(ref cmd);

and that's it! 
SqlCommandFilters supports queries that are already partially parametrized as well as true dynamic text.

Also included is a sandcastle help system project and a driver project that tests many different types of queries..

Please be sure and drop me a note if you use this project!

Enjoy!
boB 'The Toolman' Taylor
blog: aka.ms/sqlbobt
twitter: @sqlboBT
LinkedIn: https://www.linkedin.com/in/sqlbobt

Last edited Aug 24, 2016 at 1:44 PM by MajikbyboB, version 3
