#!/bin/bash

DATA_IMPORTED="DATA_ALREADY_IMPORTED"

if [ ! -e $DATA_IMPORTED ]; then
    touch $DATA_IMPORTED
    echo "-- first container startup --"

    python3 initial_import.py
else
    echo "-- database already imported --"
fi
