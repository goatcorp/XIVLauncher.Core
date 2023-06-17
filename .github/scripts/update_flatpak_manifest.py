import yaml
import sys
import xml.etree.ElementTree as ET
from datetime import date

print("Ran with args: " + str(sys.argv))
if len(sys.argv) != 5:
    print(f'Usage: python3 {sys.argv[0]} <manifest file> <appstream file> <new tag> <new commit>')
    sys.exit(1)

manifestFile = sys.argv[1]
appstreamFile = sys.argv[2]
newTag = sys.argv[3]
newCommit = sys.argv[4]

# Read the manifest file and update the tag and commit
with open(manifestFile, 'r') as f:
    yamlFile = yaml.safe_load(f)
    manifestUpdateSuccess = False
    try:
        for module in yamlFile['modules']:
            if module['name'] == 'xivlauncher':
                for source in module['sources']:
                    if source['url'] == 'https://github.com/goatcorp/XIVLauncher.Core.git':
                        source['tag'] = newTag
                        source['commit'] = newCommit
                        with open(manifestFile, 'w') as f:
                            yaml.dump(yamlFile, f)
                            manifestUpdateSuccess = True
                            break
    except KeyError:
        pass

    if manifestUpdateSuccess is False:
        print('Error: failed to update XIVLauncher.Core manifest.. exiting')
        sys.exit(1)

    print('Updated XIVLauncher.Core manifest')

# Read the appstream file and update the tag and commit
with open(appstreamFile, 'r') as f:
    tree = ET.parse(f)
    root = tree.getroot()
    appstreamUpdateSuccess = False
    for component in root:
        if component.tag == 'releases':
            for release in component:
                release.set('date', date.today().strftime("%Y-%m-%d"))
                release.set('version', newTag)
                tree.write(appstreamFile)
                appstreamUpdateSuccess = True
                break

    if appstreamUpdateSuccess is False:
        print('Error: failed to update appstream file.. exiting')
        sys.exit(1)

    print('Updated appstream file')

# Python XML parser doesn't add the XML header, so we need to add it manually
with open(appstreamFile, 'r') as f:
    lines = f.readlines()
    lines.insert(0, '<?xml version="1.0" encoding="UTF-8"?>\n')
    with open(appstreamFile, 'w') as f:
        f.writelines(lines)

    print('Added XML header to appstream file')

print(f'Updated {manifestFile} and {appstreamFile} to tag {newTag} and commit {newCommit} on {date.today().strftime("%Y-%m-%d")}')
