import yaml
import sys

if len(sys.argv) != 4:
    print(f'Usage: python3 {sys.argv[0]} <config file> <new tag number> <new commit hash>')
    sys.exit(1)

fileName = sys.argv[1]
newTag = sys.argv[2]
newCommit = sys.argv[3]

# Read the given file and try to parse it to update its tag and commit.
with open(fileName, 'r') as f:
    config = yaml.safe_load(f)
    success = False
    try:
        for module in config['modules']:
            if module['name'] == 'xivlauncher':
                for source in module['sources']:
                    if source['url'] == 'https://github.com/goatcorp/XIVLauncher.Core.git':
                        source['tag'] = newTag
                        source['commit'] = newCommit
                        with open(fileName, 'w') as f:
                            yaml.dump(config, f)
                            success = True
                            break;
    except KeyError:
        pass

    if success is False:
        print('Error: failed to update XIVLauncher.Core commit and tag.. exiting')
        sys.exit(1)

    print(f'Updated XIVLauncher.Core tag to {newTag} and commit to {newCommit} in {fileName}')
