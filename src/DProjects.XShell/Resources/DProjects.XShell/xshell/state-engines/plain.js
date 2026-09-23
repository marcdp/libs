// export
export default function createStateEngineFactoryPlain(stateSkeleton, context) {
	
	// init state definition json
	const stateSkeletonJson = JSON.stringify(stateSkeleton || {});
	
	// returns a state instance factory
	return {
		create: () => {
			return JSON.parse(stateSkeletonJson);
		}
	};
	
}