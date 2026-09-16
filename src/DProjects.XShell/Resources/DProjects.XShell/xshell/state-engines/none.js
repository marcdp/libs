// export
export default function createStateEngineFactoryNone(stateSkeleton, stateDefinition) {

	// returns a state instance factory
	return {
		create: () => {
			return null;
		}
	};
}