from flask import Flask
from flask import render_template, request
from flask_restful import reqparse, abort, Api, Resource
from pisces import pisces_api

app = Flask(__name__)
api = Api(app)

@app.route('/pisces')
def pisces():
    return render_template('pisces.html')

@app.route('/pisces2')
def pisces2():
    return render_template('pisces2.html')

@app.route('/', methods=['GET'])
def index():
    return render_template('pisces.html')

class fishhucs(Resource):
    def get(self, hucid):
        huc = []
        huc.append(hucid)
        return pisces_api.get_fish_by_huc(huc)

class fishRangeBySpecies(Resource):
    def get(self, speciesid):
        species = []
        species.append(speciesid)
        return pisces_api.get_fish_range_by_species(species)

class getStreamSegmentID(Resource):
    def get(self, latitude, longitude):
        return pisces_api.getStreamSegmentShape(latitude, longitude)

class getStreamSegmentShape(Resource):
    def post(self):
        data = request.get_json(force=True)
        latitude  = data["latitude"]
        longitude = data["longitude"]
        return pisces_api.getStreamSegmentShape(latitude, longitude)

class getEcoRegionFromLngLat(Resource):
    def post(self):
        data = request.get_json(force=True)
        latitude  = data["latitude"]
        longitude = data["longitude"]
        return pisces_api.getEcoRegionFromLngLat(longitude, latitude)



api.add_resource(fishhucs, '/fishhucs/<string:hucid>')
api.add_resource(fishRangeBySpecies, '/fishrange/<string:speciesid>')
api.add_resource(getStreamSegmentShape, '/streamsegment')
api.add_resource(getEcoRegionFromLngLat, '/ecoregion')

if __name__ == '__main__':
    import argparse

    parser = argparse.ArgumentParser(description='Development Server Help')
    parser.add_argument("-d", "--debug", action="store_true", dest="debug_mode",
                        help="run in debug mode (for use with PyCharm)", default=False)
    parser.add_argument("-p", "--port", dest="port",
                        help="port of server (default:%(default)s)", type=int, default=5000)

    cmd_args = parser.parse_args()
    app_options = {"port": cmd_args.port}

    if cmd_args.debug_mode:
        app_options["debug"] = True
        app_options["use_debugger"] = False
        app_options["use_reloader"] = False

    app.run(**app_options)
