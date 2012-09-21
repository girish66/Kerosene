KEROSENE ORM
------------

Kerosene is a dynamic, self-adaptive, and configuration less library, with full support
for POCO objects, and for natural SQL-like syntax while coding your database operations
from your source code.

You are not forced any longer to maintain long and complex external configuration files,
bacause Kerosene will adapt itself to whatever schema your database has, or to whatever
set of results your particular command is returning.

For instance, you are not even supposed to know the actual detauls of those tables and
columns in your database except just their names (just for some very special scenarios
you may want to know their types). The beauty of this is that if the schema of your
database changes for whatever reason, as far as those names don't change your solution
will continue to perform as expected.

Kerosene has two main modes of operation:

- "Core" mode, in which your solution will basically deal basically with the records as
they are returned from the database, and it is well adapted to intensive data operations,

- and "Maps" mode, in which you have a complete Entity Framework specifically built for
your POCO classes, that does not require any modification on those (and not even in the
form of attributes added to them because, at the end of the day, it may very well happen
that you don't have access to their source code).

An introductory article can be found at:

http://www.codeproject.com/Articles/117666/Kerosene-ORM-a-dynamic-self-adaptive-and-configura

Full documentation can be found at:

http://KeroseneORM.codeplex.com/

Enjoy.
