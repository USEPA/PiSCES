FROM python:3

RUN apt-get -qq update && \
    apt-get install -y sqlite3 libsqlite3-dev libspatialite-dev spatialite-bin

#RUN apt-get install -y libspatialite-dev spatialite-bin
#RUN rm -rf /var/cache/apk/*

# Install Python Dependencies
COPY requirements.txt /tmp/
RUN pip install --requirement /tmp/requirements.txt

# Install uWSGI
RUN pip install uwsgi

# Overwrite the uWSGI config
COPY uwsgi.ini /etc/uwsgi/

COPY . /src/
WORKDIR /src
EXPOSE 8080

CMD ["uwsgi", "/etc/uwsgi/uwsgi.ini"]