import sqlite3


def execute_select_query(query, include_headers=None):
    """
    Generic sqlite3 select database query

    Arg1: database name (and path if needed)
    Arg2: query

    Returns: list       
    """

    database = "pisces.db"
    conn = sqlite3.connect(database)
    cursor = conn.cursor()
    rows = cursor.execute(query)
        
    table = []    
    if  include_headers == True:
        headers = list(map(lambda x: x[0], cursor.description))
        table.append(headers)
        #names2 = [description[0] for description in cursor.description]
    
    for row in rows:
        table.append(row)

    conn.close()

    return table