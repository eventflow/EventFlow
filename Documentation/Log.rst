.. _log:

Log
===

The default log implementation of EventFLow logs to the console. To have another
behavior, register an implementation of ``ILog``, use the ``Log`` as a base class
to make the implementation easier.