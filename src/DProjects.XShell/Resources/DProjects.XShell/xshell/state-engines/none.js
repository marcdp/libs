// export
export default function createStateEngineFactoryNone(stateSkeleton, context) {

	// returns a state instance factory
	return {
		create: () => {
			return null;
		}
	};
	
}