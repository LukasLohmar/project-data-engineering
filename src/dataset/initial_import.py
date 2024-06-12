import psycopg2
import pandas as pd
from sqlalchemy import create_engine, text

# create db connection
conn = psycopg2.connect("User ID=postgres;Password=postgres;Server=localhost;Port=6501;Database=data-system-db; Integrated Security=true;Pooling=true;")

# create a cursor
cur = conn.cursor()

# automatic commit
conn.set_session(autocommit=True)



# commit and close connection
conn.commit()
cur.close()
conn.close()
