import sqlite3
import os
import sys
import inspect

def execute_select_query(query, include_headers=None):
    """
    Generic sqlite3 select database query

    Arg1: database name (and path if needed)
    Arg2: query

    Returns: list       
    """

    curpath = inspect.getfile(inspect.currentframe())

    location = os.path.realpath(os.path.join(os.getcwd(), os.path.dirname(__file__)))

    database = os.path.join(location, 'pisces.sqlite')
    print('Pisces.sqlite location:' + database)

    try:
        #currDir = os.getcwd()
        #os.chdir(location)
        #database = "sqlitemgr/pisces.db"
        conn = sqlite3.connect(database)
        cursor = conn.cursor()
        rows = cursor.execute(query)
        #os.chdir(currDir)
    except sqlite3.Error as e:
        msg = e.args[0]
        print('Exception in execute_select_query: ' + msg)
        raise

    table = []    
    if  include_headers == True:
        headers = list(map(lambda x: x[0], cursor.description))
        table.append(headers)
        #names2 = [description[0] for description in cursor.description]
    
    for row in rows:
        table.append(row)

    conn.close()

    return table