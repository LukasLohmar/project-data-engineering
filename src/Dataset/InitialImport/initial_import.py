import csv
import uuid
import os
from datetime import datetime
from decimal import Decimal

import psycopg2.extras

# use uuid
psycopg2.extras.register_uuid()
psycopg2.extras.register_ipaddress()

# create db connection
conn = psycopg2.connect(dbname=os.environ['POSTGRES_DB'], user=os.environ['POSTGRES_USER'], password=os.environ['POSTGRES_PASSWORD'], host=os.environ['POSTGRES_HOST'], port=os.environ['POSTGRES_PORT'])

# create a cursor
cur = conn.cursor()

# automatic commit
conn.set_session(autocommit=True)

user_token = uuid.uuid4()

print('generated token: ', user_token)

# set first access-token and link dataset entries to it
cur.execute("""INSERT INTO "Authorization" ("Token", "Locked", "AuthorizedFlags", "CreatedAt") VALUES (%s, %s, %s, %s);""", (user_token, False, 6, datetime.now()))

# open csv file
with open('./iot_telemetry_data.csv', newline='') as csvfile:
    row_reader = csv.reader(csvfile, delimiter=',', quotechar='|', quoting=csv.QUOTE_NONE)
    # skip header
    next(row_reader, None)

    data = []

    for row in row_reader:
        timestamp = row[0]
        split_ts = timestamp.split('E')
        # timestamp is like this: 0.00123E5 but python expects 0.00123E+05
        timestamp = split_ts[0] + 'E+0' + split_ts[1]
        timestamp = datetime.fromtimestamp(float(timestamp.replace('"', '')))

        device_id = row[1].replace('"', '')
        carbon_dioxide = Decimal(row[2].replace('"', ''))
        humidity = Decimal(row[3].replace('"', ''))
        light = row[4].replace('"', '') == 'true'
        lpg = Decimal(row[5].replace('"', ''))
        motion = row[6].replace('"', '') == 'true'
        smoke = Decimal(row[7].replace('"', ''))
        temperature = Decimal(row[8].replace('"', ''))

        token_id = 1

        # unify values and round to 1 digit
        data.append((timestamp, device_id, carbon_dioxide, round(humidity, 1), light, lpg, motion, smoke, round(temperature, 1), token_id))

    # batch insert values
    psycopg2.extras.execute_values(cur, """INSERT INTO "SensorData" ("TimeStamp", "DeviceId", "CarbonDioxide", "Humidity", "Light",
        "Lpg", "Motion", "Smoke", "Temperature", "ProviderTokenId") VALUES %s;""", data)

    print('inserted ', len(data), ' values')
# commit and close connection
conn.commit()
cur.close()
conn.close()
